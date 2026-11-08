// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Configuration;
using Cratis.Identity.Resources;
using Cratis.Identity.Tenants;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization.TokenExchange;

/// <summary>
/// Handles an RFC 8693 token exchange after OpenIddict has authenticated the client and checked its grant, scope
/// and resource permissions. Fails closed: every identity-side failure is the same <c language="text">invalid_grant</c>
/// so a caller cannot tell which check failed, and nothing is issued unless every check passed.
/// </summary>
/// <param name="validator">The <see cref="ISubjectTokenValidator"/>.</param>
/// <param name="replay">The <see cref="SubjectTokenReplayGuard"/>.</param>
/// <param name="keys">The <see cref="IUpstreamKeyResolver"/>.</param>
/// <param name="accounts">The <see cref="IAccountReader"/>.</param>
/// <param name="resources">The <see cref="IResourceCatalog"/>.</param>
/// <param name="tenants">The <see cref="ITenantDirectory"/>.</param>
/// <param name="options">The <see cref="IdentityOptions"/>.</param>
/// <param name="time">The <see cref="TimeProvider"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
internal class TokenExchangeGrant(
    ISubjectTokenValidator validator,
    SubjectTokenReplayGuard replay,
    IUpstreamKeyResolver keys,
    IAccountReader accounts,
    IResourceCatalog resources,
    ITenantDirectory tenants,
    IOptions<IdentityOptions> options,
    TimeProvider time,
    ILogger<TokenExchangeGrant> logger)
{
    /// <summary>
    /// The one description every identity-side refusal carries.
    /// </summary>
    internal const string InvalidSubjectDescription = "The subject token is not valid.";

    /// <summary>
    /// The longest request-supplied value written to a log.
    /// </summary>
    internal const int MaximumLoggedLength = 64;

    /// <summary>
    /// Truncates a caller-supplied value and strips control characters so it is safe to log.
    /// </summary>
    /// <param name="value">The value as sent.</param>
    /// <returns>The value limited to <see cref="MaximumLoggedLength"/> characters.</returns>
    public static string SafeForLog(string value)
    {
        var clean = new string([.. value.Take(MaximumLoggedLength).Select(_ => char.IsControl(_) ? '?' : _)]);
        return value.Length > MaximumLoggedLength ? clean + "…" : clean;
    }

    /// <summary>
    /// Handles a token-exchange request.
    /// </summary>
    /// <param name="request">The <see cref="OpenIddictRequest"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="IResult"/>.</returns>
    public async Task<IResult> Handle(OpenIddictRequest request, CancellationToken cancellationToken = default)
    {
        var client = options.Value.TokenExchange.Clients.SingleOrDefault(_ => string.Equals(_.ClientId, request.ClientId, StringComparison.Ordinal));
        if (client is null)
        {
            return OAuthResults.Error(Errors.UnauthorizedClient, "The client is not allowed to use this grant type.");
        }

        if (ParseParameters(request) is not { } parameters)
        {
            return OAuthResults.Error(Errors.InvalidRequest, "The token exchange request is malformed.");
        }

        var resource = resources.FindByIndicator(parameters.Resource);
        if (resource is null || !client.Resources.Contains(resource.Audience, StringComparer.Ordinal))
        {
            return OAuthResults.Error(Errors.InvalidTarget, "The requested resource is not available to this client.");
        }

        if (parameters.Scopes.Any(_ => string.Equals(_, Scopes.OfflineAccess, StringComparison.Ordinal) || !resource.Owns(_) || (client.Scopes.Count > 0 && !client.Scopes.Contains(_, StringComparer.Ordinal))))
        {
            return OAuthResults.Error(Errors.InvalidScope, "The requested scope is not available to this client for this resource.");
        }

        var subject = await validator.Validate(parameters.SubjectToken, client, cancellationToken);
        if (subject.Refusal == SubjectTokenRefusal.KeysUnavailable)
        {
            logger.Refused(client.ClientId, subject.Refusal);
            return OAuthResults.Error(Errors.TemporarilyUnavailable, "Token exchange is unavailable right now. Try again later.");
        }

        if (subject.Subject is not { } verified)
        {
            logger.Refused(client.ClientId, subject.Refusal);
            return Invalid();
        }

        var account = await ResolveAccount(verified, cancellationToken);
        if (account is null)
        {
            logger.RefusedForIssuer(client.ClientId, SubjectTokenRefusal.Invalid, verified.Issuer.Value, "not-linked");
            return Invalid();
        }

        var lookup = await tenants.SelectableTenants(account, resource, cancellationToken);
        if (lookup is not TenantsListed listed)
        {
            logger.TenantsUnavailable(client.ClientId, account.Id, resource.Audience);
            return OAuthResults.Error(Errors.TemporarilyUnavailable, "Tenant selection is unavailable right now. Try again later.");
        }

        if (!listed.Tenants.Any(_ => string.Equals(_.Id, parameters.Tenant, StringComparison.Ordinal)))
        {
            logger.TenantNotSelectable(client.ClientId, account.Id, SafeForLog(parameters.Tenant), resource.Audience);
            return Invalid();
        }

        var issuerOptions = options.Value.TokenExchange.Issuers.Single(_ => string.Equals(_.Issuer, verified.Issuer.Value, StringComparison.Ordinal));
        var remembered = verified.Expiry < verified.IssuedAt + issuerOptions.MaxTokenAge ? verified.Expiry : verified.IssuedAt + issuerOptions.MaxTokenAge;
        switch (replay.Record(verified.ReplayKey, remembered + issuerOptions.ClockSkew))
        {
            case ReplayRecordResult.AlreadyUsed:
                logger.RefusedForIssuer(client.ClientId, SubjectTokenRefusal.Replayed, verified.Issuer.Value, "replay");
                return Invalid();
            case ReplayRecordResult.Full:
                logger.RefusedForIssuer(client.ClientId, SubjectTokenRefusal.ReplayGuardFull, verified.Issuer.Value, "replay-guard-full");
                return OAuthResults.Error(Errors.TemporarilyUnavailable, "Token exchange is unavailable right now. Try again later.");
        }

        var lifetime = options.Value.Tokens.AccessTokenLifetime;
        var remaining = verified.Expiry - time.GetUtcNow();
        if (remaining < lifetime)
        {
            lifetime = remaining;
        }

        return OAuthResults.Issue(AccessTokenIdentity.Exchange(
            account,
            client.ClientId,
            resource,
            parameters.Tenant,
            parameters.Scopes,
            verified.Issuer.Value,
            verified.Subject.Value,
            verified.AuthTime,
            lifetime));
    }

    static IResult Invalid() => OAuthResults.Error(Errors.InvalidGrant, InvalidSubjectDescription);

    static ExchangeParameters? ParseParameters(OpenIddictRequest request)
    {
        if (request.GetParameter(TokenExchangeConstants.ActorToken) is not null ||
            request.GetParameter(TokenExchangeConstants.Audience) is not null)
        {
            return null;
        }

        var requested = Single(request, TokenExchangeConstants.RequestedTokenType);
        var subjectTokenType = Single(request, TokenExchangeConstants.SubjectTokenType);
        var subjectToken = Single(request, TokenExchangeConstants.SubjectToken);
        var resource = Single(request, Parameters.Resource);
        var tenant = Single(request, TokenExchangeConstants.Tenant);
        var scopes = request.GetScopes();
        var typeAllowed = string.Equals(subjectTokenType, TokenExchangeConstants.AccessTokenType, StringComparison.Ordinal) || string.Equals(subjectTokenType, TokenExchangeConstants.JwtTokenType, StringComparison.Ordinal);
        var requestedAllowed = request.GetParameter(TokenExchangeConstants.RequestedTokenType) is null || string.Equals(requested, TokenExchangeConstants.AccessTokenType, StringComparison.Ordinal);
        if (!typeAllowed || !requestedAllowed || string.IsNullOrEmpty(subjectToken) || string.IsNullOrEmpty(resource) || string.IsNullOrEmpty(tenant) || scopes.IsDefaultOrEmpty)
        {
            return null;
        }

        return new(subjectToken, resource, tenant, [.. scopes]);
    }

    static string? Single(OpenIddictRequest request, string name) =>
        request.GetParameter(name) is { Count: 0 } parameter ? (string?)parameter : null; // A repeated parameter is a multi-valued one (Count > 0) and is refused.

    async Task<Account?> ResolveAccount(ValidatedSubject subject, CancellationToken cancellationToken)
    {
        var match = await keys.Find(subject.Issuer, subject.Subject);
        return match is { Active: true } ? await accounts.FindActive(match.AccountSubject, cancellationToken) : null;
    }

    sealed record ExchangeParameters(string SubjectToken, string Resource, string Tenant, string[] Scopes);
}

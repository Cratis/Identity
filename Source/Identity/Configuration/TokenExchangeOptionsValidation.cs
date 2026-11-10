// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients;

namespace Cratis.Identity.Configuration;

/// <summary>
/// Validates <see cref="TokenExchangeOptions"/> as part of the startup validation of <see cref="IdentityOptions"/>.
/// </summary>
internal static class TokenExchangeOptionsValidation
{
    const int MinimumSecretLength = 32;

    static readonly string[] _asymmetricAlgorithms = ["RS256", "RS384", "RS512", "PS256", "PS384", "PS512", "ES256", "ES384", "ES512"];

    /// <summary>
    /// Validates the token-exchange configuration.
    /// </summary>
    /// <param name="options">The <see cref="IdentityOptions"/>.</param>
    /// <param name="isDevelopment">Whether the host runs in Development.</param>
    /// <returns>The failures; empty when the configuration is valid.</returns>
    internal static IEnumerable<string> Validate(IdentityOptions options, bool isDevelopment)
    {
        var exchange = options.TokenExchange;
        var failures = new List<string>();
        failures.AddRange(Duplicates(exchange.Issuers.Select(_ => _.Issuer), "token-exchange issuer"));
        failures.AddRange(Duplicates(exchange.Clients.Select(_ => _.ClientId), "token-exchange client"));
        foreach (var client in exchange.Clients.Where(_ => options.IntrospectionClients.Any(i => string.Equals(i.ClientId, _.ClientId, StringComparison.Ordinal)) ||
            string.Equals(_.ClientId, ClientDescriptors.CratisCli, StringComparison.Ordinal)))
        {
            failures.Add($"The token-exchange client \"{client.ClientId}\" collides with another client.");
        }

        foreach (var issuer in exchange.Issuers)
        {
            failures.AddRange(ValidateIssuer(options, issuer, isDevelopment));
        }

        foreach (var client in exchange.Clients)
        {
            failures.AddRange(ValidateClient(options, client, isDevelopment));
        }

        return failures;
    }

    static IEnumerable<string> ValidateIssuer(IdentityOptions options, ExternalIssuerOptions issuer, bool isDevelopment) =>
        ValidateIssuerIdentity(options, issuer.Issuer)
            .Concat(ValidateJwksUri(issuer, isDevelopment))
            .Concat(ValidateIssuerTokenPolicy(issuer))
            .Concat(ValidateIssuerTiming(issuer));

    static IEnumerable<string> ValidateIssuerIdentity(IdentityOptions options, string name)
    {
        if (!Uri.TryCreate(name, UriKind.Absolute, out var uri) || !ValidIssuerUri(uri))
        {
            yield return $"The token-exchange issuer \"{name}\" must be an absolute HTTPS URI without user info or a fragment.";
        }

        if (!string.IsNullOrWhiteSpace(options.Issuer) && string.Equals(name?.TrimEnd('/'), options.Issuer.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
        {
            yield return $"The token-exchange issuer \"{name}\" must not be Identity's own issuer.";
        }
    }

    static bool ValidIssuerUri(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.Fragment) && string.IsNullOrEmpty(uri.UserInfo);

    static IEnumerable<string> ValidateJwksUri(ExternalIssuerOptions issuer, bool isDevelopment)
    {
        var name = issuer.Issuer;
        if (!Uri.TryCreate(issuer.JwksUri, UriKind.Absolute, out var jwks) || !ValidJwksUri(jwks))
        {
            yield return $"The JWKS URI of token-exchange issuer \"{name}\" must be an absolute HTTP(S) URI without user info or a fragment.";
        }
        else if (!isDevelopment && jwks.Scheme != Uri.UriSchemeHttps)
        {
            yield return $"The JWKS URI of token-exchange issuer \"{name}\" requires HTTPS outside Development.";
        }
    }

    static bool ValidJwksUri(Uri uri) =>
        (uri.Scheme == "http" || uri.Scheme == "https") && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Fragment);

    static IEnumerable<string> ValidateIssuerTokenPolicy(ExternalIssuerOptions issuer)
    {
        var name = issuer.Issuer;
        if (issuer.Audiences.Count == 0 || issuer.Audiences.Any(string.IsNullOrWhiteSpace))
        {
            yield return $"Token-exchange issuer \"{name}\" needs at least one non-empty audience.";
        }

        if (issuer.SigningAlgorithms.Count == 0 || issuer.SigningAlgorithms.Any(_ => !_asymmetricAlgorithms.Contains(_, StringComparer.Ordinal)))
        {
            yield return $"Token-exchange issuer \"{name}\" accepts only asymmetric signing algorithms (RS*, PS*, ES*); none and HMAC algorithms are refused.";
        }
    }

    static IEnumerable<string> ValidateIssuerTiming(ExternalIssuerOptions issuer)
    {
        var name = issuer.Issuer;
        if (issuer.ClockSkew < TimeSpan.Zero || issuer.ClockSkew > TimeSpan.FromSeconds(120))
        {
            yield return $"The clock skew of token-exchange issuer \"{name}\" must be between zero and two minutes.";
        }

        if (issuer.MaxTokenAge <= TimeSpan.Zero || issuer.MaxTokenAge > TimeSpan.FromHours(1))
        {
            yield return $"The maximum token age of token-exchange issuer \"{name}\" must be greater than zero and at most one hour.";
        }

        if (issuer.JwksTimeout <= TimeSpan.Zero || issuer.JwksTimeout > TimeSpan.FromSeconds(10))
        {
            yield return $"The JWKS timeout of token-exchange issuer \"{name}\" must be greater than zero and at most ten seconds.";
        }
    }

    static IEnumerable<string> ValidateClient(IdentityOptions options, TokenExchangeClientOptions client, bool isDevelopment) =>
        ValidateClientCredentials(client, isDevelopment)
            .Concat(ValidateClientIssuers(options.TokenExchange, client))
            .Concat(ValidateClientResources(options.Resources, client));

    static IEnumerable<string> ValidateClientCredentials(TokenExchangeClientOptions client, bool isDevelopment)
    {
        if (string.IsNullOrWhiteSpace(client.ClientId) || string.IsNullOrWhiteSpace(client.ClientSecret))
        {
            yield return $"Token-exchange client \"{client.ClientId}\" needs both a client id and a client secret.";
        }
        else if (!isDevelopment && client.ClientSecret.Length < MinimumSecretLength)
        {
            yield return $"The secret of token-exchange client \"{client.ClientId}\" must have at least 32 characters outside Development.";
        }
    }

    static IEnumerable<string> ValidateClientIssuers(TokenExchangeOptions exchange, TokenExchangeClientOptions client)
    {
        if (client.Issuers.Count == 0)
        {
            yield return $"Token-exchange client \"{client.ClientId}\" must name at least one issuer.";
        }

        foreach (var issuer in client.Issuers.Where(_ => !exchange.Issuers.Any(i => string.Equals(i.Issuer, _, StringComparison.Ordinal))))
        {
            yield return $"Token-exchange client \"{client.ClientId}\" references an unknown issuer \"{issuer}\".";
        }
    }

    static IEnumerable<string> ValidateClientResources(IList<ResourceServerOptions> resources, TokenExchangeClientOptions client)
    {
        if (client.Resources.Count == 0)
        {
            yield return $"Token-exchange client \"{client.ClientId}\" must name at least one resource.";
        }

        var known = resources.Where(_ => client.Resources.Contains(_.Audience, StringComparer.Ordinal)).ToArray();
        foreach (var audience in client.Resources.Where(_ => !resources.Any(r => string.Equals(r.Audience, _, StringComparison.Ordinal))))
        {
            yield return $"Token-exchange client \"{client.ClientId}\" references a resource \"{audience}\" that is not configured or was disabled by hosted configuration.";
        }

        foreach (var failure in ValidateClientScopes(known, client)) yield return failure;
    }

    static IEnumerable<string> ValidateClientScopes(ResourceServerOptions[] known, TokenExchangeClientOptions client)
    {
        foreach (var scope in client.Scopes.Where(_ => !known.Any(r => r.Scopes.Any(s => string.Equals(s.Name, _, StringComparison.Ordinal)))))
        {
            yield return $"Token-exchange client \"{client.ClientId}\" names scope \"{scope}\", which none of its resources own.";
        }
    }

    static IEnumerable<string> Duplicates(IEnumerable<string> values, string kind) =>
        values.GroupBy(_ => _, StringComparer.Ordinal).Where(_ => _.Count() > 1).Select(_ => $"The {kind} \"{_.Key}\" is configured more than once.");
}

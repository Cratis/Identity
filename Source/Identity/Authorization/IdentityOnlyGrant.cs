// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Resources;
using static OpenIddict.Abstractions.OpenIddictConstants;
using OidcScopes = OpenIddict.Abstractions.OpenIddictConstants.Scopes;

namespace Cratis.Identity.Authorization;

/// <summary>
/// Represents an identity-only authorization, with neither a resource nor a tenant binding.
/// </summary>
/// <param name="Scopes">The requested identity scopes within the configured client's ceiling.</param>
internal sealed record IdentityOnlyGrant(IEnumerable<string> Scopes) : GrantEvaluation
{
    /// <summary>Evaluates identity scopes without changing resource-grant resolution.</summary>
    /// <param name="request">The authorization request.</param>
    /// <param name="clients">The configured confidential OIDC clients.</param>
    /// <param name="resources">The resource catalog.</param>
    /// <returns>An identity decision, or null to retain the existing resource policy.</returns>
    internal static GrantEvaluation? Evaluate(GrantRequest request, IEnumerable<OidcClientOptions> clients, IResourceCatalog resources)
    {
        var scopes = request.Scopes.Distinct(StringComparer.Ordinal).ToArray();
        if (!scopes.Contains(OidcScopes.OpenId, StringComparer.Ordinal) &&
            !scopes.Contains(OidcScopes.Profile, StringComparer.Ordinal))
        {
            return null;
        }

        var client = clients.SingleOrDefault(client => string.Equals(client.ClientId, request.ClientId, StringComparison.Ordinal));
        if (client is null)
        {
            return new NotGrantable(Errors.InvalidScope, "Identity scopes are only available to registered OpenID Connect clients.");
        }

        if (scopes.Where(ProtocolScopes.Contains).Except(client.EffectiveScopes, StringComparer.Ordinal).Any())
        {
            return new NotGrantable(Errors.InvalidScope, "The requested identity scopes exceed the registered client ceiling.");
        }

        var resourceScopes = scopes.Where(scope => !ProtocolScopes.Contains(scope)).ToArray();
        if (resourceScopes.Any(scope => !resources.FindByScope(scope).Any()))
        {
            return new NotGrantable(Errors.InvalidScope, "The requested scope is not known.");
        }

        if (resourceScopes.Length == 0)
        {
            return EvaluateIdentity(request, scopes, client);
        }

        return scopes.Contains(OidcScopes.OpenId, StringComparer.Ordinal)
            ? new NotGrantable(Errors.InvalidScope, "Identity scopes cannot be combined with resource scopes.")
            : null;
    }

    static GrantEvaluation EvaluateIdentity(GrantRequest request, string[] scopes, OidcClientOptions client)
    {
        if (request.ResourceIndicators.Any())
        {
            return new NotGrantable(Errors.InvalidTarget, "Identity-only grants cannot be bound to a resource.");
        }

        if (!scopes.Contains(OidcScopes.OpenId, StringComparer.Ordinal))
        {
            return new NotGrantable(Errors.InvalidScope, "Identity-only grants require the openid scope.");
        }

        return new IdentityOnlyGrant(scopes.Intersect(client.EffectiveScopes, StringComparer.Ordinal).ToArray());
    }
}

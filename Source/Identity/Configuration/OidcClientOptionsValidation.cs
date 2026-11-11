// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Identity.Clients;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Configuration;

/// <summary>
/// Validates configured OpenID Connect clients and the issuance gate at startup.
/// </summary>
internal static class OidcClientOptionsValidation
{
    /// <summary>
    /// Validates client identities, scope ceilings, callback URIs and the recorded consent decision.
    /// </summary>
    /// <param name="options">The Identity options.</param>
    /// <returns>Validation failures without secrets or callback values.</returns>
    internal static IEnumerable<string> Validate(IdentityOptions options)
    {
        foreach (var failure in ValidateIssuance(options)) yield return failure;
        HashSet<string> ids =
        [
            with(collection: options.IntrospectionClients.Select(_ => _.ClientId), comparer: StringComparer.Ordinal),
            ClientDescriptors.CratisCli
        ];
        ids.UnionWith(options.TokenExchange.Clients.Select(_ => _.ClientId));
        foreach (var client in options.OidcClients)
        {
            if (string.IsNullOrWhiteSpace(client.ClientId))
            {
                yield return "OIDC clients require a non-empty client identifier.";
            }

            if (!ids.Add(client.ClientId))
            {
                yield return "An OIDC client identifier collides with another client.";
            }

            foreach (var failure in ValidateClient(client)) yield return failure;
        }
    }

    static IEnumerable<string> ValidateIssuance(IdentityOptions options)
    {
        if (!options.Oidc.IssuanceEnabled) yield break;
        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            yield return "Identity:Issuer must be configured when OpenID Connect issuance is enabled.";
        }
        if (options.Oidc.ConsentDecision is not ("ImplicitFencedPath" or "ActivationBarrier"))
        {
            yield return "Identity:Oidc:IssuanceEnabled requires ConsentDecision to be ImplicitFencedPath or ActivationBarrier.";
        }
    }

    static IEnumerable<string> ValidateClient(OidcClientOptions client)
    {
        if (string.IsNullOrWhiteSpace(client.ClientSecret))
        {
            yield return "OIDC clients require a non-empty secret.";
        }

        if (!client.EffectiveScopes.Contains(Scopes.OpenId, StringComparer.Ordinal) ||
            client.EffectiveScopes.Any(scope => scope is not (Scopes.OpenId or Scopes.Profile or Scopes.OfflineAccess)))
        {
            yield return "OIDC scopes must include openid and contain only openid, profile and offline_access.";
        }

        if (client.RedirectUris.Any(uri => !ValidCallback(uri)))
        {
            yield return "OIDC redirect URIs must be absolute HTTPS URIs (HTTP IP-loopback literals are permitted), without wildcards, fragments or user information.";
        }

        if (client.PostLogoutRedirectUris.Any(uri => !ValidCallback(uri)))
        {
            yield return "OIDC post-logout redirect URIs must be absolute HTTPS URIs (HTTP IP-loopback literals are permitted), without wildcards, fragments or user information.";
        }
    }

    static bool ValidCallback(string configured) =>
        !string.IsNullOrWhiteSpace(configured) && !configured.Contains('*') &&
        Uri.TryCreate(configured, UriKind.Absolute, out var uri) &&
        string.IsNullOrEmpty(uri.Fragment) && string.IsNullOrEmpty(uri.UserInfo) &&
        (uri.Scheme == Uri.UriSchemeHttps || IsLoopbackHttp(uri));

    static bool IsLoopbackHttp(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttp &&
        IPAddress.TryParse(uri.Host.Trim('[', ']'), out var address) && IPAddress.IsLoopback(address);
}

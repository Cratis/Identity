// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Clients;

/// <summary>
/// Declares confidential OpenID Connect web clients with configured scope ceilings and mandatory PKCE.
/// </summary>
internal static class OidcClientDescriptors
{
    /// <summary>
    /// Creates the descriptors without enabling identity-only issuance.
    /// </summary>
    /// <param name="clients">The configured clients.</param>
    /// <returns>The validated application descriptors.</returns>
    internal static IEnumerable<OpenIddictApplicationDescriptor> For(IEnumerable<OidcClientOptions> clients) => clients.Select(Create);

    static OpenIddictApplicationDescriptor Create(OidcClientOptions client)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = client.ClientId,
            ClientSecret = client.ClientSecret,
            DisplayName = client.DisplayName,
            ClientType = ClientTypes.Confidential,
            ApplicationType = ApplicationTypes.Web,
            ConsentType = ConsentTypes.Implicit,
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.EndSession,
                Permissions.Endpoints.Revocation,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.ResponseTypes.Code
            },
            Requirements = { Requirements.Features.ProofKeyForCodeExchange }
        };
        descriptor.RedirectUris.UnionWith(client.RedirectUris.Select(uri => new Uri(uri, UriKind.Absolute)));
        descriptor.PostLogoutRedirectUris.UnionWith(client.PostLogoutRedirectUris.Select(uri => new Uri(uri, UriKind.Absolute)));
        descriptor.Permissions.UnionWith(client.EffectiveScopes.Select(scope => Permissions.Prefixes.Scope + scope));
        if (client.EffectiveScopes.Contains(Scopes.OfflineAccess, StringComparer.Ordinal))
        {
            descriptor.Permissions.Add(Permissions.GrantTypes.RefreshToken);
        }

        var validation = ClientRegistrationRules.Validate(descriptor, [CodeChallengeMethods.Sha256]);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(string.Join(' ', validation.Refusals));
        }

        return descriptor;
    }
}

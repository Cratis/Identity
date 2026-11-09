// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.Configuration;
using Cratis.Identity.Resources;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Clients;

/// <summary>
/// Declares the clients Identity knows. In v1 the read-only application store serves these descriptors directly.
/// </summary>
/// <param name="resources">The <see cref="IResourceCatalog"/>.</param>
/// <param name="options">The <see cref="IdentityOptions"/>.</param>
internal class ClientDescriptors(IResourceCatalog resources, IOptions<IdentityOptions> options)
{
    /// <summary>
    /// The client identifier of the first-party Cratis CLI.
    /// </summary>
    public const string CratisCli = "cratis-cli";

    /// <summary>
    /// Gets the descriptors of all clients.
    /// </summary>
    /// <returns>The <see cref="OpenIddictApplicationDescriptor"/> instances.</returns>
    public IEnumerable<OpenIddictApplicationDescriptor> All() =>
        [CratisCliDescriptor(), .. options.Value.IntrospectionClients.Select(IntrospectionClient), .. options.Value.TokenExchange.Clients.Select(ExchangeClient)];

    /// <summary>
    /// Creates the descriptor of the first-party Cratis CLI: a public native client (no secret) using the
    /// authorization code flow with PKCE and RFC 8252 loopback redirects. OpenIddict accepts any port on the
    /// registered loopback redirect URIs for native clients, and requires the host to match exactly, so only
    /// the IP literals are registered and <c language="text">localhost</c> is refused.
    /// </summary>
    /// <returns>The <see cref="OpenIddictApplicationDescriptor"/>.</returns>
    /// <exception cref="InvalidOperationException">The built-in descriptor violates the registration rules.</exception>
    public OpenIddictApplicationDescriptor CratisCliDescriptor()
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = CratisCli,
            ClientType = ClientTypes.Public,
            ApplicationType = ApplicationTypes.Native,
            ConsentType = ConsentTypes.Explicit,
            DisplayName = "Cratis CLI",
            RedirectUris =
            {
                new Uri("http://127.0.0.1/callback"),
                new Uri("http://[::1]/callback")
            },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.Revocation,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code
            },
            Requirements =
            {
                Requirements.Features.ProofKeyForCodeExchange
            }
        };

        if (options.Value.DeviceGrant.Enabled)
        {
            descriptor.Permissions.Add(Permissions.Endpoints.DeviceAuthorization);
            descriptor.Permissions.Add(Permissions.GrantTypes.DeviceCode);
        }

        foreach (var scope in resources.All.SelectMany(_ => _.Scopes))
        {
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + scope.Name);
        }

        foreach (var indicator in resources.All.SelectMany(_ => _.Indicators))
        {
            descriptor.Permissions.Add(Permissions.Prefixes.Resource + indicator);
        }

        var validation = ClientRegistrationRules.Validate(descriptor, [CodeChallengeMethods.Sha256]);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(string.Join(' ', validation.Refusals));
        }

        return descriptor;
    }

    static OpenIddictApplicationDescriptor IntrospectionClient(IntrospectionClientOptions client) => new()
    {
        ClientId = client.ClientId,
        ClientSecret = client.ClientSecret,
        ClientType = ClientTypes.Confidential,
        DisplayName = client.ClientId,
        Permissions = { Permissions.Endpoints.Introspection }
    };

    /// <summary>
    /// Creates the descriptor of a token-exchange client: confidential, token endpoint and the exchange grant only,
    /// so it can neither redeem a code nor hold a refresh token, limited to its resources and scope ceiling.
    /// </summary>
    /// <param name="client">The <see cref="TokenExchangeClientOptions"/>.</param>
    /// <returns>The <see cref="OpenIddictApplicationDescriptor"/>.</returns>
    /// <exception cref="InvalidOperationException">The descriptor violates the registration rules.</exception>
    OpenIddictApplicationDescriptor ExchangeClient(TokenExchangeClientOptions client)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = client.ClientId,
            ClientSecret = client.ClientSecret,
            ClientType = ClientTypes.Confidential,
            DisplayName = client.ClientId,
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.Prefixes.GrantType + TokenExchangeConstants.GrantType
            }
        };

        var allowed = client.Resources.Select(resources.FindByAudience).OfType<ResourceServer>().ToArray();
        foreach (var scope in client.Scopes.Count > 0 ? client.Scopes : [.. allowed.SelectMany(_ => _.Scopes).Select(_ => _.Name)])
        {
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + scope);
        }

        foreach (var indicator in allowed.SelectMany(_ => _.Indicators))
        {
            descriptor.Permissions.Add(Permissions.Prefixes.Resource + indicator);
        }

        var validation = ClientRegistrationRules.Validate(descriptor, [CodeChallengeMethods.Sha256]);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(string.Join(' ', validation.Refusals));
        }

        return descriptor;
    }
}

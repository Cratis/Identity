// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients;
using Cratis.Identity.Configuration;
using Cratis.Identity.Resources;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Integration.given;

/// <summary>
/// The shipped clients plus a second device-capable client, because the protocol stores are configuration-backed
/// and read-only at runtime.
/// </summary>
/// <param name="resources">The resource catalog.</param>
/// <param name="options">The Identity options.</param>
internal sealed class SpecClientDescriptors(IResourceCatalog resources, IOptions<IdentityOptions> options) : ClientDescriptors(resources, options)
{
    /// <summary>The client id of the extra device-capable client.</summary>
    public const string OtherDeviceClient = "device-binding-spec";

    /// <summary>The confidential web client with openid and profile scopes.</summary>
    public const string OidcWeb = "oidc-web";

    /// <summary>The confidential web client whose scope ceiling is openid alone.</summary>
    public const string OidcOpenIdOnly = "oidc-openid-only";

    /// <summary>The second confidential web client for cross-client negative cases.</summary>
    public const string OidcOther = "oidc-other";

    /// <summary>The synthetic secret used only by OIDC integration fixtures.</summary>
    public const string OidcClientSecret = "unused-in-specs";

    /// <summary>Adds the three test OIDC registrations to a fixture's configuration.</summary>
    /// <param name="settings">The fixture settings.</param>
    internal static void AddOidcClients(Dictionary<string, string?> settings)
    {
        var clients = new[] { OidcWeb, OidcOpenIdOnly, OidcOther };
        for (var index = 0; index < clients.Length; index++)
        {
            var prefix = $"Identity:OidcClients:{index}";
            settings[$"{prefix}:ClientId"] = clients[index];
            settings[$"{prefix}:ClientSecret"] = OidcClientSecret;
            settings[$"{prefix}:DisplayName"] = clients[index];
            settings[$"{prefix}:RedirectUris:0"] = "https://rp.example/signin-oidc";
            settings[$"{prefix}:PostLogoutRedirectUris:0"] = "https://rp.example/signout-callback-oidc";
            settings[$"{prefix}:Scopes:0"] = Scopes.OpenId;
            if (clients[index] != OidcOpenIdOnly)
            {
                settings[$"{prefix}:Scopes:1"] = Scopes.Profile;
            }
        }
    }

    /// <inheritdoc/>
    public override IEnumerable<OpenIddictApplicationDescriptor> All() =>
    [
        .. base.All(),
        new OpenIddictApplicationDescriptor
        {
            ClientId = OtherDeviceClient,
            ClientType = ClientTypes.Public,
            Permissions = { Permissions.Endpoints.Token, Permissions.GrantTypes.DeviceCode }
        }
    ];
}

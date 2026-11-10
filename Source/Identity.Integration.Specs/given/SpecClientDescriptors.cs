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
/// <param name="development">The local-development profile, when a spec host enables it.</param>
internal sealed class SpecClientDescriptors(IResourceCatalog resources, IOptions<IdentityOptions> options, DevelopmentOptions? development = null) : ClientDescriptors(resources, options, development)
{
    /// <summary>The client id of the extra device-capable client.</summary>
    public const string OtherDeviceClient = "device-binding-spec";

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

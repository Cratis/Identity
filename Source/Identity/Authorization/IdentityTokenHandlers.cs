// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization;

/// <summary>Registers only the explicitly enabled OIDC profile.</summary>
internal static class IdentityTokenHandlers
{
    /// <summary>Adds narrowly ordered OIDC validation, signing, and metadata handlers.</summary>
    /// <param name="server">The authorization server builder.</param>
    public static void AddTo(OpenIddictServerBuilder server) => server
        .AddEventHandler(ValidateOidcRequest.Descriptor)
        .AddEventHandler(RefuseOidcIssuance.Descriptor)
        .AddEventHandler(IdentityTokenSigning.Descriptor)
        .AddEventHandler(IdentityTokenMetadata.Descriptor);
}

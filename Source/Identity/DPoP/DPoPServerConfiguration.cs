// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.DPoP;

/// <summary>Registers only the opt-in token-endpoint profile.</summary>
internal static class DPoPServerConfiguration
{
    /// <summary>Leaves the default server pipeline unchanged when disabled.</summary>
    /// <param name="server">The server builder.</param>
    /// <param name="enabled">Whether the host opted into DPoP.</param>
    /// <returns>The configured builder for chaining.</returns>
    internal static OpenIddictServerBuilder AddDPoPHandlers(this OpenIddictServerBuilder server, bool enabled)
    {
        if (!enabled) return server;
        return server.AddEventHandler(ValidateDPoPProof.Descriptor)
            .AddEventHandler(AttachDPoPConfirmation.Descriptor)
            .AddEventHandler(ApplyDPoPTokenResponse.Descriptor)
            .AddEventHandler(AttachDPoPMetadata.Descriptor);
    }
}

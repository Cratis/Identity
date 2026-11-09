// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.DPoP;

/// <summary>Advertises the enabled profile, without implying resource-server support.</summary>
internal sealed class AttachDPoPMetadata : IOpenIddictServerHandler<HandleConfigurationRequestContext>
{
    static readonly string[] _algorithms = ["ES256"];

    /// <summary>Gets the metadata descriptor.</summary>
    internal static OpenIddictServerHandlerDescriptor Descriptor { get; } = OpenIddictServerHandlerDescriptor.CreateBuilder<HandleConfigurationRequestContext>()
        .UseSingletonHandler<AttachDPoPMetadata>()
        .SetOrder(int.MaxValue - 100_000)
        .Build();

    /// <inheritdoc/>
    public ValueTask HandleAsync(HandleConfigurationRequestContext context)
    {
        context.Metadata["dpop_signing_alg_values_supported"] = new OpenIddictParameter(JsonSerializer.SerializeToElement(_algorithms));
        return ValueTask.CompletedTask;
    }
}

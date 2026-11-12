// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Authorization;

/// <summary>Advertises the ID-token signing contract, independently of OAuth access-token algorithms.</summary>
internal sealed class IdentityTokenMetadata : IOpenIddictServerHandler<HandleConfigurationRequestContext>
{
    /// <summary>Gets the descriptor, after the native algorithm catalogue.</summary>
    public static OpenIddictServerHandlerDescriptor Descriptor { get; } = OpenIddictServerHandlerDescriptor.CreateBuilder<HandleConfigurationRequestContext>()
        .UseScopedHandler<IdentityTokenMetadata>()
        .SetOrder(OpenIddictServerHandlers.Discovery.AttachSigningAlgorithms.Descriptor.Order + 500)
        .Build();

    /// <inheritdoc/>
    public ValueTask HandleAsync(HandleConfigurationRequestContext context)
    {
        context.IdTokenSigningAlgorithms.Clear();
        context.IdTokenSigningAlgorithms.Add(SecurityAlgorithms.RsaSha256);
        return ValueTask.CompletedTask;
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.DPoP;

/// <summary>Overrides only the successful response scheme, not the JWT token type.</summary>
/// <param name="binding">The admitted binding.</param>
internal sealed class ApplyDPoPTokenResponse(DPoPBinding binding) : IOpenIddictServerHandler<ApplyTokenResponseContext>
{
    /// <summary>Gets the descriptor before ASP.NET response serialization.</summary>
    internal static OpenIddictServerHandlerDescriptor Descriptor { get; } = OpenIddictServerHandlerDescriptor.CreateBuilder<ApplyTokenResponseContext>()
        .UseScopedHandler<ApplyDPoPTokenResponse>()
        .SetOrder(int.MinValue + 100_000)
        .Build();

    /// <inheritdoc/>
    public ValueTask HandleAsync(ApplyTokenResponseContext context)
    {
        if (binding.Jkt is not null && string.IsNullOrEmpty(context.Response.Error) && !string.IsNullOrEmpty(context.Response.AccessToken)) context.Response.TokenType = "DPoP";
        return ValueTask.CompletedTask;
    }
}

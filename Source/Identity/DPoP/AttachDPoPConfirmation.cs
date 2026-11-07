// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.DPoP;

/// <summary>Attaches object-valued confirmation only to the prepared access-token principal.</summary>
/// <param name="binding">The admitted request binding.</param>
internal sealed class AttachDPoPConfirmation(DPoPBinding binding) : IOpenIddictServerHandler<ProcessSignInContext>
{
    /// <summary>Gets the descriptor after access-token preparation, before serialization.</summary>
    internal static OpenIddictServerHandlerDescriptor Descriptor { get; } = OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessSignInContext>()
        .UseScopedHandler<AttachDPoPConfirmation>()
        .SetOrder(OpenIddictServerHandlers.PrepareAccessTokenPrincipal.Descriptor.Order + 1)
        .Build();

    /// <inheritdoc/>
    public ValueTask HandleAsync(ProcessSignInContext context)
    {
        if (context.AccessTokenPrincipal is { } principal)
        {
            // Never carry a previous cnf forward from an incoming principal.
            principal.SetClaims("cnf", []);
            if (binding.Jkt is { } jkt) principal.SetClaim("cnf", JsonSerializer.SerializeToElement(new { jkt }));
        }

        return ValueTask.CompletedTask;
    }
}

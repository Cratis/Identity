// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Authorization;

/// <summary>Refuses issuance and server-side token use while continuity or restriction coverage is unproven.</summary>
/// <param name="gate">The host recovery gate.</param>
/// <param name="log">The independent epoch authority.</param>
internal sealed class RefuseIssuanceDuringRecovery(RecoveryGate gate, IRecoveryLog log) : IOpenIddictServerHandler<ProcessSignInContext>, IOpenIddictServerHandler<ValidateTokenContext>, IOpenIddictServerHandler<ProcessRequestContext>
{
    /// <summary>Gets the final issuance barrier.</summary>
    internal static OpenIddictServerHandlerDescriptor Descriptor { get; } = OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessSignInContext>()
        .UseScopedHandler<RefuseIssuanceDuringRecovery>()
        .SetOrder(OpenIddictServerHandlers.ValidateSignInDemand.Descriptor.Order - 2)
        .SetType(OpenIddictServerHandlerType.Custom).Build();

    /// <summary>Gets the server-token-use barrier.</summary>
    internal static OpenIddictServerHandlerDescriptor ValidationDescriptor { get; } = OpenIddictServerHandlerDescriptor.CreateBuilder<ValidateTokenContext>()
        .UseScopedHandler<RefuseIssuanceDuringRecovery>()
        .SetOrder(int.MinValue + 100)
        .SetType(OpenIddictServerHandlerType.Custom).Build();

    /// <summary>Gets the authorization, token and introspection admission barrier.</summary>
    internal static OpenIddictServerHandlerDescriptor RequestDescriptor { get; } = OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessRequestContext>()
        .UseScopedHandler<RefuseIssuanceDuringRecovery>()
        .SetOrder(OpenIddictServerHandlers.InferEndpointType.Descriptor.Order + 1)
        .SetType(OpenIddictServerHandlerType.Custom).Build();

    /// <inheritdoc/>
    public async ValueTask HandleAsync(ProcessRequestContext context)
    {
        if (context.EndpointType is OpenIddictServerEndpointType.Authorization or OpenIddictServerEndpointType.Token or OpenIddictServerEndpointType.Introspection &&
            !await CanUse(context.CancellationToken))
        {
            context.Reject(error: Errors.ServerError, description: "The server is recovering its restriction state.");
        }
    }

    /// <inheritdoc/>
    public async ValueTask HandleAsync(ProcessSignInContext context)
    {
        if (!await CanUse(context.CancellationToken))
        {
            context.Reject(error: Errors.ServerError, description: "The server is recovering its restriction state.");
        }
    }

    /// <inheritdoc/>
    public async ValueTask HandleAsync(ValidateTokenContext context)
    {
        if (context.Transaction.EndpointType != OpenIddictServerEndpointType.Revocation && !await CanUse(context.CancellationToken))
        {
            context.Reject(error: Errors.ServerError, description: "The server is recovering its restriction state.");
        }
    }

    async Task<bool> CanUse(CancellationToken cancellationToken)
    {
        try
        {
            var epoch = await log.RecoveryEpoch(cancellationToken);
            if (epoch != gate.Epoch)
            {
                gate.Close();
                gate.ObserveEpoch(epoch);
            }

            return gate.Ready;
        }
        catch (Exception)
        {
            gate.Close();
            return false;
        }
    }
}

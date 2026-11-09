// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Authorization;

/// <summary>Refuses issuance and server-side token use while continuity or restriction coverage is unproven.</summary>
/// <param name="gate">The host recovery gate.</param>
/// <param name="log">The independent epoch authority.</param>
/// <param name="tokens">The operational token manager.</param>
/// <param name="authorizations">The operational authorization manager.</param>
internal sealed class RefuseIssuanceDuringRecovery(RecoveryGate gate, IRecoveryLog log, IOpenIddictTokenManager tokens, IOpenIddictAuthorizationManager authorizations) : IOpenIddictServerHandler<ProcessSignInContext>, IOpenIddictServerHandler<ValidateTokenContext>, IOpenIddictServerHandler<ProcessRequestContext>
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

    /// <summary>Gets the epoch check after entry identifiers have been resolved.</summary>
    internal static OpenIddictServerHandlerDescriptor EntryValidationDescriptor { get; } = OpenIddictServerHandlerDescriptor.CreateBuilder<ValidateTokenContext>()
        .AddFilter<OpenIddictServerHandlerFilters.RequireDegradedModeDisabled>()
        .AddFilter<OpenIddictServerHandlerFilters.RequireTokenStorageEnabled>()
        .AddFilter<OpenIddictServerHandlerFilters.RequireTokenIdResolved>()
        .UseScopedHandler<RefuseIssuanceDuringRecovery>()
        .SetOrder(RejectFencedGrants.Descriptor.Order - 1)
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
        else if (!await EntriesCurrent(context.Principal?.GetTokenId(), context.Principal?.GetAuthorizationId(), context.CancellationToken))
        {
            context.Reject(error: Errors.InvalidGrant, description: "The authorization is no longer valid.");
        }
    }

    /// <inheritdoc/>
    public async ValueTask HandleAsync(ValidateTokenContext context)
    {
        if (context.Transaction.EndpointType == OpenIddictServerEndpointType.Revocation) return;
        if (context.Transaction.EndpointType == OpenIddictServerEndpointType.Introspection)
        {
            // Preserve the first observation: validation can dispatch this barrier more than once.
            context.Transaction.Properties.TryAdd(IntrospectionCredentialClaims.RecoveryEpochProperty, gate.Epoch);
        }

        if (!await CanUse(context.CancellationToken, validation: true))
        {
            context.Reject(error: Errors.ServerError, description: "The server is recovering its restriction state.");
        }
        else if (!await EntriesCurrent(context.TokenId, context.AuthorizationId, context.CancellationToken))
        {
            context.Reject(
                error: context.EndpointType == OpenIddictServerEndpointType.Token ? Errors.InvalidGrant : Errors.InvalidToken,
                description: "The authorization is no longer valid.");
        }
    }

    async Task<bool> EntriesCurrent(string? tokenId, string? authorizationId, CancellationToken cancellationToken)
    {
        try
        {
            var epoch = gate.Epoch;
            if (!string.IsNullOrEmpty(tokenId))
            {
                var token = await tokens.FindByIdAsync(tokenId, cancellationToken);
                if (token is null || !RecoveryEpochStamp.IsCurrent(await tokens.GetPropertiesAsync(token, cancellationToken), epoch)) return false;
            }

            if (!string.IsNullOrEmpty(authorizationId))
            {
                var authorization = await authorizations.FindByIdAsync(authorizationId, cancellationToken);
                if (authorization is null || !RecoveryEpochStamp.IsCurrent(await authorizations.GetPropertiesAsync(authorization, cancellationToken), epoch)) return false;
            }

            return gate.Allows(RestrictionClass.Tokens) && gate.Allows(RestrictionClass.Authorizations) && gate.Epoch == epoch;
        }
        catch (Exception)
        {
            return false;
        }
    }

    async Task<bool> CanUse(CancellationToken cancellationToken, bool validation = false)
    {
        try
        {
            var epoch = await log.RecoveryEpoch(cancellationToken);
            if (epoch != gate.Epoch)
            {
                gate.Close();
                gate.ObserveEpoch(epoch);
            }

            return gate.Allows(RestrictionClass.Grants) && gate.Allows(RestrictionClass.Tokens) &&
                gate.Allows(RestrictionClass.Authorizations) && (validation || gate.Allows(RestrictionClass.Accounts));
        }
        catch (Exception)
        {
            gate.Close();
            return false;
        }
    }
}

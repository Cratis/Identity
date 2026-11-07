// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Authorization;

/// <summary>Consults primary authority before token-entry validation; no worker or Chronicle availability is required.</summary>
/// <param name="fences">The authoritative grant store.</param>
/// <param name="tokens">The entry manager.</param>
/// <param name="read">The tightly constrained legacy-family bootstrap.</param>
/// <param name="grants">The retryable restriction protocol for RFC 7009 acknowledgement.</param>
/// <param name="issuance">The request-scoped successor binding.</param>
internal sealed class RejectFencedGrants(IFenceStore fences, IOpenIddictTokenManager tokens, ReadRefreshGrant read, GrantRevocation grants, RefreshIssuance issuance) : IOpenIddictServerHandler<ValidateTokenContext>, IOpenIddictServerHandler<ProcessSignInContext>
{
    /// <summary>Gets the descriptor, before reuse handling and built-in token-entry validation.</summary>
    public static OpenIddictServerHandlerDescriptor Descriptor { get; } = OpenIddictServerHandlerDescriptor.CreateBuilder<ValidateTokenContext>()
        .AddFilter<OpenIddictServerHandlerFilters.RequireDegradedModeDisabled>()
        .AddFilter<OpenIddictServerHandlerFilters.RequireTokenStorageEnabled>()
        .AddFilter<OpenIddictServerHandlerFilters.RequireTokenIdResolved>()
        .UseScopedHandler<RejectFencedGrants>()
        .SetOrder(RevokeGrantOnRefreshTokenReuse.Descriptor.Order - 1)
        .SetType(OpenIddictServerHandlerType.Custom).Build();

    /// <summary>Gets the final response guard after stock redemption and before successor generation.</summary>
    public static OpenIddictServerHandlerDescriptor SignInDescriptor { get; } = OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessSignInContext>()
        .UseScopedHandler<RejectFencedGrants>()
        .SetOrder(OpenIddictServerHandlers.RedeemTokenEntry.Descriptor.Order + 2)
        .SetType(OpenIddictServerHandlerType.Custom).Build();

    /// <inheritdoc/>
    public async ValueTask HandleAsync(ValidateTokenContext context)
    {
        if (string.IsNullOrEmpty(context.AuthorizationId))
        {
            return;
        }

        bool denied;
        try
        {
            var state = await fences.Read(context.AuthorizationId, context.CancellationToken);
            if (state is not null && !GrantStateValidation.IsKnown(state))
            {
                throw new RevocationPending();
            }

            if (context.EndpointType is OpenIddictServerEndpointType.Revocation)
            {
                // RFC 7009 converts invalid_token into success. Resume a tombstone's unconfirmed log BEFORE
                // allowing built-in validation to produce that response; errors here must remain server failures.
                if (state?.Fence is { } fence)
                {
                    await grants.Revoke(context.AuthorizationId, fence.Reason, context.CancellationToken);
                }

                return;
            }

            if (context.Principal?.HasTokenType(TokenTypeIdentifiers.RefreshToken) == true)
            {
                var entry = string.IsNullOrEmpty(context.TokenId) ? null : await tokens.FindByIdAsync(context.TokenId, context.CancellationToken);
                state ??= entry is null ? null : await read.Read(context.AuthorizationId, context.TokenId!, entry, context.Principal, context.CancellationToken);
                denied = state is null || state.Fence is not null || entry is null ||
                    (state.CurrentRefreshTokenId != context.TokenId && !await tokens.HasStatusAsync(entry, Statuses.Redeemed, context.CancellationToken));

                // A known Redeemed non-current entry proceeds to the reuse protocol, never to issuance.
            }
            else
            {
                denied = state?.Fence is not null;
            }
        }
        catch (Exception)
        {
            if (context.EndpointType is OpenIddictServerEndpointType.Revocation)
            {
                throw new RevocationPending();
            }

            denied = true;
        }

        if (denied)
        {
            context.Reject(error: context.EndpointType is OpenIddictServerEndpointType.Token ? Errors.InvalidGrant : Errors.InvalidToken, description: "The refresh token is no longer valid.");
        }
    }

    /// <inheritdoc/>
    public async ValueTask HandleAsync(ProcessSignInContext context)
    {
        if (context.EndpointType is not OpenIddictServerEndpointType.Token || context.Request?.IsRefreshTokenGrantType() != true)
        {
            return;
        }

        try
        {
            var state = string.IsNullOrEmpty(issuance.GrantId) ? null : await fences.Read(issuance.GrantId, context.CancellationToken);
            if (state is not null && GrantStateValidation.IsKnown(state) && state.Fence is null &&
                !string.IsNullOrEmpty(issuance.SuccessorId) && state.CurrentRefreshTokenId == issuance.SuccessorId)
            {
                return;
            }
        }
        catch (Exception)
        {
            // Unknown admission can never become a successful sign-in response.
            context.Reject(error: Errors.InvalidGrant, description: "The refresh token is no longer valid.");
            return;
        }

        context.Reject(error: Errors.InvalidGrant, description: "The refresh token is no longer valid.");
    }
}

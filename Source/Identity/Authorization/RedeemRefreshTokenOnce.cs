// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.DPoP;
using Cratis.Identity.DPoP.Operational;
using Cratis.Identity.Revocation;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Authorization;

/// <summary>
/// Atomically redeems a refresh token before OpenIddict issues its successor, and fails closed when it cannot.
/// </summary>
/// <remarks>
/// OpenIddict 7.7.1 ignores a failed redemption of a refresh token in its built-in <c language="csharp">RedeemTokenEntry</c>
/// handler, so two requests that both passed token validation could each receive a successor. This handler
/// reloads the token entry, requires it to still be valid, and redeems it through the store's optimistic
/// concurrency check (a conditional replace in MongoDB), so at most one request across all replicas wins.
/// Every loser revokes the grant's authorization and all of its tokens and is refused with
/// <c language="text">invalid_grant</c>: a successor the winner creates later still belongs to the revoked authorization and
/// fails validation. Concurrent use of a refresh token is therefore treated exactly like reuse.
/// </remarks>
/// <param name="tokens">The <see cref="IOpenIddictTokenManager"/>.</param>
/// <param name="grants">The <see cref="GrantRevocation"/>.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
/// <param name="fences">The single-document authority.</param>
/// <param name="read">The constrained legacy-family repair.</param>
/// <param name="issuance">The request-scoped successor binding.</param>
/// <param name="dpop">The optional DPoP admission service.</param>
internal class RedeemRefreshTokenOnce(IOpenIddictTokenManager tokens, GrantRevocation grants, TimeProvider timeProvider, IFenceStore fences, ReadRefreshGrant read, RefreshIssuance issuance, DPoPAdmission? dpop = null) : IOpenIddictServerHandler<ProcessSignInContext>
{
    /// <summary>
    /// Gets the descriptor. It runs just before OpenIddict's redemption handler, whose later attempt to redeem the
    /// already redeemed token is harmless; this handler alone decides whether a refresh request may issue tokens.
    /// </summary>
    public static OpenIddictServerHandlerDescriptor Descriptor { get; } =
        OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessSignInContext>()
            .UseScopedHandler<RedeemRefreshTokenOnce>()
            .SetOrder(OpenIddictServerHandlers.RedeemTokenEntry.Descriptor.Order - 1)
            .SetType(OpenIddictServerHandlerType.Custom)
            .Build();

    /// <inheritdoc/>
    public async ValueTask HandleAsync(ProcessSignInContext context)
    {
        if (!IsRefreshTokenRequest(context))
        {
            return;
        }

        var presented = await Resolve(context);
        if (presented is null)
        {
            return;
        }

        var state = await TryReadState(context, presented);
        if (state is null || state.Fence is not null)
        {
            context.Reject(error: Errors.InvalidGrant, description: "The refresh token is no longer valid.");
            return;
        }

        if (await IsRedeemable(context, presented.Token, state, presented.TokenId) && await AdmitBinding(context, presented.Token))
        {
            await ConsumeAndInstall(context, presented.Token, state, presented.TokenId);
        }
    }

    static bool IsRefreshTokenRequest(ProcessSignInContext context) =>
        context.EndpointType is OpenIddictServerEndpointType.Token && context.Request?.IsRefreshTokenGrantType() == true;

    static PresentedRefreshToken? Invalid(ProcessSignInContext context)
    {
        context.Reject(error: Errors.InvalidGrant, description: "The refresh token is no longer valid.");
        return null;
    }

    async Task<PresentedRefreshToken?> Resolve(ProcessSignInContext context)
    {
        var authentication = context.Transaction.GetProperty<ProcessAuthenticationContext>(typeof(ProcessAuthenticationContext).FullName!);
        var principal = authentication?.RefreshTokenPrincipal;
        if (principal?.GetTokenId() is not { Length: > 0 } tokenId || principal.GetAuthorizationId() is not { Length: > 0 } authorizationId)
        {
            return Invalid(context);
        }

        if (await tokens.FindByIdAsync(tokenId, context.CancellationToken) is not { } token)
        {
            return Invalid(context);
        }

        // The entry has the last word on expiry: it may have expired since validation. Expiry is not reuse, so the grant stays.
        if (await HasExpired(context, token))
        {
            context.Reject(error: Errors.InvalidGrant, description: "The refresh token has expired.");
            return null;
        }

        return new PresentedRefreshToken(token, principal, tokenId, authorizationId);
    }

    async Task<GrantState?> TryReadState(ProcessSignInContext context, PresentedRefreshToken presented)
    {
        try
        {
            return await read.Read(presented.AuthorizationId, presented.TokenId, presented.Token, presented.Principal, context.CancellationToken);
        }
        catch (Exception)
        {
            return null;
        }
    }

    async Task<bool> HasExpired(ProcessSignInContext context, object token) =>
        await tokens.GetExpirationDateAsync(token, context.CancellationToken) is not { } expiration || expiration <= timeProvider.GetUtcNow();

    async Task<bool> IsRedeemable(ProcessSignInContext context, object token, GrantState state, string tokenId)
    {
        var valid = await tokens.HasStatusAsync(token, Statuses.Valid, context.CancellationToken);
        if (state.CurrentRefreshTokenId == tokenId && valid)
        {
            return true;
        }

        if (valid || await tokens.HasStatusAsync(token, Statuses.Redeemed, context.CancellationToken))
        {
            await grants.RevokeAfterReuse(state.GrantId, context.CancellationToken);
        }

        context.Reject(error: Errors.InvalidGrant, description: "The refresh token was already used.");
        return false;
    }

    async Task<bool> AdmitBinding(ProcessSignInContext context, object token)
    {
        var properties = await tokens.GetPropertiesAsync(token, context.CancellationToken);
        string? jkt = null;
        if (properties.TryGetValue(DPoPBindingStamp.PropertyName, out var stamp))
        {
            if (stamp.ValueKind != System.Text.Json.JsonValueKind.String || string.IsNullOrEmpty(stamp.GetString()))
            {
                context.Reject(Errors.InvalidGrant, "The refresh token is no longer valid.");
                return false;
            }

            jkt = stamp.GetString();
        }

        if (dpop is not null) return await dpop.Admit(context, refresh: true, existingJkt: jkt);
        if (jkt is null) return true;

        // Disabling DPoP must never downgrade an existing bound family.
        context.Reject(Errors.InvalidGrant, "The refresh token is no longer valid.");
        return false;
    }

    async Task ConsumeAndInstall(ProcessSignInContext context, object token, GrantState state, string tokenId)
    {
        var authorizationId = state.GrantId;

        // The one-document CAS, not the later token status write, serializes consume+install with revoke.
        // A crash after it strands the admitted successor; it never makes the consumed entry current again.
        var successor = FencedTokenStore.AllocateId();
        var outcome = await TryInstall(new RedeemRequest(authorizationId, tokenId, successor, state.Version), context.CancellationToken);
        if (outcome != RedeemOutcome.Installed)
        {
            await RejectFailedInstall(context, authorizationId, outcome);
            return;
        }

        if (!await tokens.TryRedeemAsync(token, context.CancellationToken))
        {
            await grants.RevokeAfterReuse(authorizationId, context.CancellationToken);
            context.Reject(error: Errors.InvalidGrant, description: "The refresh token was already used.");
            return;
        }

        issuance.Admit(state, successor);
        context.Transaction.SetProperty(typeof(RefreshIssuance).FullName!, successor);
    }

    async Task<RedeemOutcome> TryInstall(RedeemRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await Install(request, cancellationToken);
        }
        catch (Exception)
        {
            return RedeemOutcome.Unknown;
        }
    }

    async Task RejectFailedInstall(ProcessSignInContext context, string authorizationId, RedeemOutcome outcome)
    {
        if (outcome is RedeemOutcome.NotCurrent)
        {
            await grants.RevokeAfterReuse(authorizationId, context.CancellationToken);
        }

        context.Reject(error: outcome == RedeemOutcome.VersionConflict ? Errors.TemporarilyUnavailable : Errors.InvalidGrant, description: "The refresh token is no longer valid.");
    }

    async Task<RedeemOutcome> Install(RedeemRequest request, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var outcome = await fences.RedeemAndInstall(request.GrantId, request.EntryId, request.SuccessorId, request.Version, cancellationToken);
            if (outcome != RedeemOutcome.VersionConflict)
            {
                return outcome;
            }

            var current = await fences.Read(request.GrantId, cancellationToken);
            if (current is null || !GrantStateValidation.IsKnown(current))
            {
                return RedeemOutcome.Unknown;
            }

            if (current.Fence is not null)
            {
                return RedeemOutcome.Fenced;
            }

            if (current.CurrentRefreshTokenId != request.EntryId)
            {
                return RedeemOutcome.NotCurrent;
            }

            request = request with { Version = current.Version };
        }

        return RedeemOutcome.VersionConflict;
    }

    readonly record struct RedeemRequest(string GrantId, string EntryId, string SuccessorId, long Version);

    sealed record PresentedRefreshToken(object Token, ClaimsPrincipal Principal, string TokenId, string AuthorizationId);
}

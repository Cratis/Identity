// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Device;

/// <summary>Enforces persistent RFC 8628 polling intervals after code validation.</summary>
/// <param name="rates">The shared polling windows.</param>
/// <param name="tokens">The validated token entries.</param>
internal sealed class DevicePollingGuard(DeviceGrantRateLimiter rates, IOpenIddictTokenManager tokens) : IOpenIddictServerHandler<ValidateTokenContext>
{
    /// <summary>Gets the descriptor after token validation but before pending/denied status rejection.</summary>
    internal static OpenIddictServerHandlerDescriptor Descriptor { get; } = OpenIddictServerHandlerDescriptor.CreateBuilder<ValidateTokenContext>()
        .UseScopedHandler<DevicePollingGuard>()
        .SetOrder(OpenIddictServerHandlers.Protection.ValidateTokenEntry.Descriptor.Order - 1)
        .SetType(OpenIddictServerHandlerType.Custom).Build();

    /// <inheritdoc/>
    public async ValueTask HandleAsync(ValidateTokenContext context)
    {
        if (!CanPoll(context) || string.IsNullOrEmpty(context.TokenId)) return;
        if (!await HasPollableTokenEntry(context.TokenId, context.CancellationToken)) return;
        var slowed = await rates.Poll(context.TokenId, context.CancellationToken);
        if (slowed is null) context.Reject(Errors.TemporarilyUnavailable, "Device authorization is unavailable.");
        else if (slowed.Value) context.Reject(Errors.SlowDown, "The polling interval has increased by five seconds.");
    }

    /// <summary>Checks the presenter because OpenIddict defers that validation to Exchange.ValidateAuthorizedParty.</summary>
    /// <param name="context">The validation context containing the validated device code.</param>
    /// <returns>Whether the device code belongs to this client and can be polled.</returns>
    static bool CanPoll(ValidateTokenContext context) =>
        context.EndpointType == OpenIddictServerEndpointType.Token && context.Request is { } request && request.IsDeviceCodeGrantType() &&
        !context.IsRejected && context.Principal is { } principal &&
        principal.HasTokenType(TokenTypeIdentifiers.Private.DeviceCode) &&
        !string.IsNullOrEmpty(request.ClientId) && principal.HasPresenter(request.ClientId);

    /// <summary>Checks that the entry exists and is not revoked, redeemed, or otherwise invalid.</summary>
    /// <param name="id">The validated token entry identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Whether the entry can be polled before OpenIddict checks pending/denied status.</returns>
    async ValueTask<bool> HasPollableTokenEntry(string id, CancellationToken cancellationToken)
    {
        var token = await tokens.FindByIdAsync(id, cancellationToken);
        return token is not null &&
            (await tokens.HasStatusAsync(token, Statuses.Inactive, cancellationToken) ||
             await tokens.HasStatusAsync(token, Statuses.Valid, cancellationToken) ||
             await tokens.HasStatusAsync(token, Statuses.Rejected, cancellationToken));
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Authorization;

/// <summary>
/// Refuses a token whose stored entry has expired, or has no expiry, even when the entry is still in the store.
/// </summary>
/// <remarks>
/// MongoDB deletes expired entries through a TTL index, but its monitor runs about once a minute, so an expired
/// authorization code or refresh token could otherwise still be redeemed for that long. The entry, not a claim in
/// the token, is authoritative: this holds for every token type, including reference tokens whose payload lives
/// only in the entry. Expiry is not reuse, so the grant is not revoked.
/// </remarks>
/// <param name="tokens">The <see cref="IOpenIddictTokenManager"/>.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
public class RejectExpiredTokenEntries(IOpenIddictTokenManager tokens, TimeProvider timeProvider) : IOpenIddictServerHandler<ValidateTokenContext>
{
    /// <summary>
    /// Gets the descriptor. It carries the same filters as OpenIddict's token-entry validation and runs before it and before
    /// <see cref="RevokeGrantOnRefreshTokenReuse"/>.
    /// </summary>
    public static OpenIddictServerHandlerDescriptor Descriptor { get; } =
        OpenIddictServerHandlerDescriptor.CreateBuilder<ValidateTokenContext>()
            .AddFilter<OpenIddictServerHandlerFilters.RequireDegradedModeDisabled>()
            .AddFilter<OpenIddictServerHandlerFilters.RequireTokenStorageEnabled>()
            .AddFilter<OpenIddictServerHandlerFilters.RequireTokenIdResolved>()
            .UseScopedHandler<RejectExpiredTokenEntries>()
            .SetOrder(OpenIddictServerHandlers.Protection.ValidateTokenEntry.Descriptor.Order - 2)
            .SetType(OpenIddictServerHandlerType.Custom)
            .Build();

    /// <inheritdoc/>
    public async ValueTask HandleAsync(ValidateTokenContext context)
    {
        if (string.IsNullOrEmpty(context.TokenId) ||
            await tokens.FindByIdAsync(context.TokenId, context.CancellationToken) is not { } token)
        {
            // An unknown entry is left to OpenIddict's own token-entry validation.
            return;
        }

        if (await tokens.GetExpirationDateAsync(token, context.CancellationToken) is { } expiration && expiration > timeProvider.GetUtcNow())
        {
            return;
        }

        context.Reject(
            error: context.EndpointType is OpenIddictServerEndpointType.Token ? Errors.InvalidGrant : Errors.InvalidToken,
            description: "The token has expired.");
    }
}

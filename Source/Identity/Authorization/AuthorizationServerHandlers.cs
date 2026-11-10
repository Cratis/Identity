// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Authorization;

/// <summary>Configures the authorization server's ordered non-device handler chain.</summary>
internal static class AuthorizationServerHandlers
{
    /// <summary>Adds issuance, validation and refresh handlers in protocol order.</summary>
    /// <param name="server">The server builder.</param>
    /// <param name="hasSecretStore">Whether issuance requires a usable secret-store key.</param>
    /// <returns>The server builder for continuation.</returns>
    internal static OpenIddictServerBuilder AddAuthorizationHandlers(this OpenIddictServerBuilder server, bool hasSecretStore)
    {
        if (hasSecretStore)
        {
            server.AddEventHandler(RefuseIssuanceWithoutUsableKey.Descriptor);
        }

        return server.AddEventHandler(RefuseIssuanceDuringRecovery.Descriptor)
            .AddEventHandler(RefuseIssuanceDuringRecovery.ValidationDescriptor)
            .AddEventHandler(RefuseIssuanceDuringRecovery.EntryValidationDescriptor)
            .AddEventHandler(RefuseIssuanceDuringRecovery.RequestDescriptor)
            .AddEventHandler(IntrospectionCredentialClaims.Descriptor)
            .AddEventHandler(RejectExpiredTokenEntries.Descriptor)
            .AddEventHandler(RejectFencedGrants.Descriptor)
            .AddEventHandler(RejectFencedGrants.SignInDescriptor)
            .AddEventHandler(RevokeGrantOnRefreshTokenReuse.Descriptor)
            .AddEventHandler(RedeemRefreshTokenOnce.Descriptor)
            .AddEventHandler(RevokeGrantOnRefreshTokenRevocation.Descriptor);
    }

    /// <summary>Rejects explicitly empty resource indicators without requiring an indicator to be supplied.</summary>
    /// <param name="server">The server builder.</param>
    /// <returns>The server builder for continuation.</returns>
    internal static OpenIddictServerBuilder AddExplicitResourceValidation(this OpenIddictServerBuilder server) =>
        server.AddEventHandler<ValidateAuthorizationRequestContext>(builder => builder
            .SetOrder(int.MinValue + 100)
            .UseInlineHandler(RejectEmptyResource))
            .AddEventHandler<ValidateTokenRequestContext>(builder => builder
                .SetOrder(int.MinValue + 100)
                .UseInlineHandler(context => context.Request.IsAuthorizationCodeGrantType()
                    ? RejectEmptyResource(context)
                    : ValueTask.CompletedTask));

    static ValueTask RejectEmptyResource(BaseValidatingContext context)
    {
        if (context.Transaction.Request?.Resources is { } indicators && indicators.Any(string.IsNullOrEmpty))
        {
            context.Reject(error: Errors.InvalidTarget, description: "The requested resource is not known.");
        }

        return ValueTask.CompletedTask;
    }
}

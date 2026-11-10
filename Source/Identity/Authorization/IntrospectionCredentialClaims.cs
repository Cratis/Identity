// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts;
using Cratis.Identity.Revocation;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Authorization;

/// <summary>Adds authoritative grant metadata only after OpenIddict validates the token and introspection caller.</summary>
/// <param name="recovery">The realm's independently observed recovery state.</param>
internal sealed class IntrospectionCredentialClaims(RecoveryGate recovery) : IOpenIddictServerHandler<HandleIntrospectionRequestContext>
{
    /// <summary>The transaction epoch captured before introspection token validation.</summary>
    internal const string RecoveryEpochProperty = "Cratis.Identity.IntrospectionRecoveryEpoch";

    /// <summary>Gets the descriptor, after the built-in metadata and application claims.</summary>
    public static OpenIddictServerHandlerDescriptor Descriptor { get; } = OpenIddictServerHandlerDescriptor.CreateBuilder<HandleIntrospectionRequestContext>()
        .UseScopedHandler<IntrospectionCredentialClaims>()
        .SetOrder(OpenIddictServerHandlers.Introspection.AttachApplicationClaims.Descriptor.Order + 1_000)
        .SetType(OpenIddictServerHandlerType.Custom).Build();

    /// <inheritdoc/>
    public ValueTask HandleAsync(HandleIntrospectionRequestContext context)
    {
        // OpenIddict 7.8 dispatches this context only after successful validation; it has no Active property.
        // Invalid/revoked tokens take the error pipeline and become exactly {"active":false} there.
        if (!CanAttach(context))
        {
            return ValueTask.CompletedTask;
        }

        var epoch = context.Transaction.Properties.GetValueOrDefault(RecoveryEpochProperty) is long observed ? observed : -1;
        if (!IsCurrent(epoch))
        {
            Reject(context);
            return ValueTask.CompletedTask;
        }

        var principal = context.GenericTokenPrincipal;
        var credentialId = CredentialId.For(principal.GetAuthorizationId());
        if (credentialId is not null)
        {
            context.Claims["credential_id"] = credentialId;
            context.Claims["credential_kind"] = "oauth-grant";
        }

        if (principal.GetClaim(IdentityClaims.TenantId) is { } tenant)
        {
            context.Claims[IdentityClaims.TenantId] = tenant;
        }

        context.Scopes.UnionWith(principal.GetScopes());
        context.Claims["cache_epoch"] = epoch;
        if (!IsCurrent(epoch)) Reject(context);

        // Second step: cnf is well-formed token-only pass-through via AttachMetadataClaims, not DPoP validation.
        // Identity does not issue it until DPoP lands. Malformed cnf errors in OpenIddict before this handler.
        return ValueTask.CompletedTask;
    }

    static void Reject(HandleIntrospectionRequestContext context)
    {
        context.Claims.Clear();
        context.Scopes.Clear();
        context.Reject(Errors.InvalidToken);
    }

    static bool CanAttach(HandleIntrospectionRequestContext context) =>
        !context.IsRejected && !context.IsRequestHandled && !context.IsRequestSkipped &&
        context.GenericTokenPrincipal is { } principal && principal.HasTokenType(TokenTypeIdentifiers.AccessToken) &&
        !string.IsNullOrEmpty(context.Request?.ClientId) && principal.HasAudience(context.Request.ClientId);

    bool IsCurrent(long epoch) => recovery.Ready && recovery.Epoch == epoch;
}

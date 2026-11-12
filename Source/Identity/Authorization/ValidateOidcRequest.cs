// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Resources;
using Microsoft.AspNetCore;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Authorization;

/// <summary>Refuses unsupported max_age and requires a nonce and a registered client before OIDC login or consent.</summary>
/// <param name="options">The validated issuer configuration.</param>
internal sealed class ValidateOidcRequest(IOptions<IdentityOptions> options) : IOpenIddictServerHandler<ValidateAuthorizationRequestContext>
{
    /// <summary>Gets the descriptor, after native nonce syntax and redirect validation.</summary>
    public static OpenIddictServerHandlerDescriptor Descriptor { get; } = OpenIddictServerHandlerDescriptor.CreateBuilder<ValidateAuthorizationRequestContext>()
        .UseScopedHandler<ValidateOidcRequest>()
        .SetOrder(OpenIddictServerHandlers.Authentication.ValidateNonceParameter.Descriptor.Order + 500)
        .Build();

    /// <inheritdoc/>
    public async ValueTask HandleAsync(ValidateAuthorizationRequestContext context)
    {
        if (await MaxAgeRequest.IsPresent(context.Transaction.GetHttpRequest()))
        {
            context.Reject(Errors.InvalidRequest, "max_age is not supported");
            return;
        }
        if (!context.Request.HasScope(Scopes.OpenId) && !context.Request.HasScope(Scopes.Profile)) return;
        if (!options.Value.Oidc.IssuanceEnabled)
        {
            context.Reject(Errors.TemporarilyUnavailable, "OpenID Connect issuance is disabled.");
        }
        else if (!options.Value.OidcClients.Any(client => string.Equals(client.ClientId, context.ClientId, StringComparison.Ordinal)))
        {
            context.Reject(Errors.InvalidScope, "Identity scopes require a registered OpenID Connect client.");
        }
        else if (context.Request.GetScopes().Any(scope => !ProtocolScopes.Contains(scope)))
        {
            context.Reject(Errors.InvalidScope, "Identity scopes cannot be combined with resource scopes.");
        }
        else if (string.IsNullOrWhiteSpace(context.Request.Nonce))
        {
            context.Reject(Errors.InvalidRequest, "OpenID Connect requests require a nonce.");
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Keys;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Authorization;

/// <summary>Refuses unavailable OIDC issuance before code/refresh redemption or token-entry creation.</summary>
/// <param name="options">The validated issuance gate.</param>
/// <param name="timeProvider">The signing clock.</param>
/// <param name="keys">The deployed ring, absent only in ephemeral development.</param>
internal sealed class RefuseOidcIssuance(IOptions<IdentityOptions> options, TimeProvider timeProvider, SecretStoreKeys? keys = null) : IOpenIddictServerHandler<ProcessSignInContext>
{
    /// <summary>Gets the descriptor, before native sign-in demand and redemption.</summary>
    public static OpenIddictServerHandlerDescriptor Descriptor { get; } = OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessSignInContext>()
        .UseScopedHandler<RefuseOidcIssuance>()
        .SetOrder(int.MinValue + 500)
        .Build();

    /// <inheritdoc/>
    public ValueTask HandleAsync(ProcessSignInContext context)
    {
        if (!context.Principal.HasScope(Scopes.OpenId)) return ValueTask.CompletedTask;
        if (!options.Value.Oidc.IssuanceEnabled || OidcSigningKey.Find(context.Options, keys, timeProvider.GetUtcNow()) is null)
        {
            context.Reject(Errors.TemporarilyUnavailable, "OpenID Connect issuance has no eligible RS256 signer.");
        }
        return ValueTask.CompletedTask;
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Configuration;
using Cratis.Identity.Keys;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Authorization;

/// <summary>Pins ID tokens to RS256 and filters their final public descriptor, without touching other tokens.</summary>
/// <param name="options">The validated issuance gate.</param>
/// <param name="timeProvider">The signing clock.</param>
/// <param name="keys">The deployed key ring, absent only for ephemeral development credentials.</param>
internal sealed class IdentityTokenSigning(IOptions<IdentityOptions> options, TimeProvider timeProvider, SecretStoreKeys? keys = null) : IOpenIddictServerHandler<GenerateTokenContext>
{
    static readonly HashSet<string> _claims = [Claims.Subject, Claims.Audience, Claims.AuthorizedParty, Claims.AuthenticationTime, Claims.Nonce, Claims.AccessTokenHash, Claims.JwtId, OidcIdentity.SessionIdClaim, Claims.Name, Claims.PreferredUsername];

    /// <summary>Gets the descriptor, after native token metadata and before JWT serialization.</summary>
    public static OpenIddictServerHandlerDescriptor Descriptor { get; } = OpenIddictServerHandlerDescriptor.CreateBuilder<GenerateTokenContext>()
        .UseScopedHandler<IdentityTokenSigning>()
        .SetOrder(OpenIddictServerHandlers.Protection.GenerateIdentityModelToken.Descriptor.Order - 500)
        .Build();

    /// <inheritdoc/>
    public ValueTask HandleAsync(GenerateTokenContext context)
    {
        if (context.TokenType != TokenTypeIdentifiers.IdentityToken) return ValueTask.CompletedTask;
        var credentials = OidcSigningKey.Find(context.Options, keys, timeProvider.GetUtcNow());
        if (!CanSign(context, credentials))
        {
            context.Reject(Errors.TemporarilyUnavailable, "OpenID Connect issuance has no eligible RS256 signer.");
            return ValueTask.CompletedTask;
        }
        var refresh = context.Transaction.Request?.IsRefreshTokenGrantType() == true;
        if (!HasRequiredClaims(context.SecurityTokenDescriptor.Subject, refresh))
        {
            context.Reject(Errors.InvalidGrant, "The identity grant has no original authentication or nonce.");
            return ValueTask.CompletedTask;
        }
        context.SigningCredentials = credentials;
        context.EncryptionCredentials = null;
        context.SecurityTokenDescriptor.SigningCredentials = credentials;
        ConfigureDescriptor(context.SecurityTokenDescriptor, context.ClientId!, options.Value.Tokens.IdentityTokenLifetime, context.Principal?.GetTokenId());
        if (refresh)
        {
            Remove(context.SecurityTokenDescriptor.Subject!, Claims.Nonce);
            context.SecurityTokenDescriptor.Claims.Remove(Claims.Nonce);
        }
        return ValueTask.CompletedTask;
    }

    bool CanSign(GenerateTokenContext context, SigningCredentials? credentials) =>
        options.Value.Oidc.IssuanceEnabled && credentials is not null && !string.IsNullOrEmpty(context.ClientId);

    static bool HasRequiredClaims(ClaimsIdentity? identity, bool refresh)
    {
        if (identity is null) return false;
        if (!refresh && string.IsNullOrWhiteSpace(identity.FindFirst(Claims.Nonce)?.Value)) return false;
        return new[] { Claims.Subject, Claims.AuthenticationTime }.All(type => !string.IsNullOrWhiteSpace(identity.FindFirst(type)?.Value));
    }

    static void Remove(ClaimsIdentity identity, string type)
    {
        foreach (var claim in identity.FindAll(type).ToArray()) identity.RemoveClaim(claim);
    }

    static void ConfigureDescriptor(SecurityTokenDescriptor descriptor, string clientId, TimeSpan lifetime, string? tokenId)
    {
        descriptor.EncryptingCredentials = null;
        descriptor.TokenType = "JWT";
        descriptor.Subject = new ClaimsIdentity(descriptor.Subject!.Claims.Where(claim => _claims.Contains(claim.Type)));
        descriptor.Claims ??= new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var claim in descriptor.Claims.Keys.Where(claim => !_claims.Contains(claim)).ToArray()) descriptor.Claims.Remove(claim);
        descriptor.Audience = null;
        descriptor.Audiences.Clear();
        descriptor.Claims[Claims.Audience] = new[] { clientId };
        descriptor.Claims[Claims.AuthorizedParty] = clientId;
        Remove(descriptor.Subject, Claims.JwtId);
        descriptor.Claims[Claims.JwtId] = tokenId ?? Guid.NewGuid().ToString("N");
        descriptor.Expires = descriptor.IssuedAt!.Value.Add(lifetime);
    }
}

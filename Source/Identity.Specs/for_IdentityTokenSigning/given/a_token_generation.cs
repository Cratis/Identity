// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json;
using Cratis.Identity.Authorization;
using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_IdentityTokenSigning.given;

public class a_token_generation : for_OidcIdentity.given.an_identity_grant
{
    RSA _rsa;
    ECDsa _ec;
    IdentityTokenSigning _handler;
    private protected GenerateTokenContext _context;
    protected JsonWebToken _jwt;
    protected JsonElement _payload;
    protected bool _signatureValid;
    protected virtual string GrantType => "authorization_code";

    void Establish()
    {
        _rsa = RSA.Create(2048);
        _ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var rsa = new SigningCredentials(new RsaSecurityKey(_rsa) { KeyId = "rsa-spec" }, SecurityAlgorithms.RsaSha256);
        var ec = new SigningCredentials(new ECDsaSecurityKey(_ec) { KeyId = "ec-spec" }, SecurityAlgorithms.EcdsaSha256);
        var options = new IdentityOptions();
        options.Oidc.IssuanceEnabled = true;
        _handler = new(Options.Create(options), new FixedTimeProvider(AuthTime));
        _context = new(new OpenIddictServerTransaction
        {
            Options = new OpenIddictServerOptions { SigningCredentials = { ec, rsa } },
            Request = new OpenIddictRequest { GrantType = GrantType, ClientId = ClientId },
            Response = new()
        }) { ClientId = ClientId, TokenType = OpenIddictConstants.TokenTypeIdentifiers.IdentityToken, SigningCredentials = ec };
        var identity = OidcIdentity.Create(new(_account, AuthTime, SessionId), ClientId, new IdentityOnlyGrant(["openid", "profile"]), "example-nonce");
        identity.SetClaim("tenant_id", "must-not-escape");
        identity.SetClaim("github_id", "must-not-escape");
        identity.SetClaim("email", "must-not-escape");
        _context.SecurityTokenDescriptor.Subject = identity;
        _context.SecurityTokenDescriptor.Issuer = "https://identity.example/";
        _context.SecurityTokenDescriptor.IssuedAt = AuthTime.UtcDateTime;
        _context.SecurityTokenDescriptor.Expires = AuthTime.AddHours(2).UtcDateTime;
        _context.SecurityTokenDescriptor.Claims = new Dictionary<string, object> { ["cid"] = "must-not-escape", ["aud"] = "wrong-audience", ["jti"] = "previous-token" };
    }

    protected async Task Generate()
    {
        await _handler.HandleAsync(_context);
        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(_context.SecurityTokenDescriptor);
        _jwt = handler.ReadJsonWebToken(token);
        _payload = JsonDocument.Parse(Base64UrlEncoder.Decode(_jwt.EncodedPayload)).RootElement.Clone();
        var validated = await handler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = "https://identity.example/", ValidAudience = ClientId,
            IssuerSigningKey = new RsaSecurityKey(_rsa), ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ValidTypes = ["JWT"], ValidateLifetime = false
        });
        _signatureValid = validated.IsValid;
    }

    void Destroy()
    {
        _rsa.Dispose();
        _ec.Dispose();
    }
}

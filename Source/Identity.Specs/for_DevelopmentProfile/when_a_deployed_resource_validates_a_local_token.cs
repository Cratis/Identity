// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cratis.Identity.Configuration;
using Cratis.Identity.Development;
using Cratis.Identity.Keys;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_a_deployed_resource_validates_a_local_token : given.a_development_profile
{
    const string DeployedIssuer = "https://id.example.com/";
    const string Audience = DevelopmentProfile.DirectMcpAudience;

    SecretStoreKeys _keys;
    string _token;
    RsaSecurityKey _localKey;
    TokenValidationResult _deployed;
    TokenValidationResult _wrongIssuerOnly;
    TokenValidationResult _unrelatedKey;
    TokenValidationResult _local;
    TokenValidationResult _restAudience;
    TokenValidationResult _restTokenOnMcp;

    async Task Because()
    {
        Apply();
        _keys = new SecretStoreKeys(new SecretStoreOptions { Path = _storeDirectory }, TimeProvider.System, NullLogger<SecretStoreKeys>.Instance, providesDataProtection: false);
        var signer = _keys.Current.Signer;
        _localKey = new RsaSecurityKey(signer.Certificate.GetRSAPublicKey()) { KeyId = signer.Kid };
        _token = Issue(signer, Audience);

        using var deployedKey = RSA.Create(2048);
        using var unrelatedKey = RSA.Create(2048);
        var handler = new JsonWebTokenHandler();
        _deployed = await handler.ValidateTokenAsync(_token, Parameters(DeployedIssuer, Audience, new RsaSecurityKey(deployedKey.ExportParameters(false))));
        _wrongIssuerOnly = await handler.ValidateTokenAsync(_token, Parameters(DeployedIssuer, Audience, _localKey));
        _unrelatedKey = await handler.ValidateTokenAsync(_token, Parameters(LocalIssuer, Audience, new RsaSecurityKey(unrelatedKey.ExportParameters(false)) { KeyId = signer.Kid }));
        _local = await handler.ValidateTokenAsync(_token, Parameters(LocalIssuer, Audience, _localKey));
        _restAudience = await handler.ValidateTokenAsync(_token, Parameters(LocalIssuer, DevelopmentProfile.DirectApiAudience, _localKey));
        _restTokenOnMcp = await handler.ValidateTokenAsync(Issue(signer, DevelopmentProfile.DirectApiAudience), Parameters(LocalIssuer, Audience, _localKey));
    }

    void Destroy() => _keys?.Dispose();

    [Fact] void should_reject_it_with_the_deployed_issuer_and_keys() => _deployed.IsValid.ShouldBeFalse();
    [Fact] void should_reject_it_with_the_deployed_issuer_even_when_the_key_matches() => _wrongIssuerOnly.IsValid.ShouldBeFalse();
    [Fact] void should_reject_it_with_the_local_issuer_and_an_unrelated_key() => _unrelatedKey.IsValid.ShouldBeFalse();
    [Fact] void should_accept_it_only_against_the_local_issuer_and_keys() => _local.IsValid.ShouldBeTrue();
    [Fact] void should_refuse_the_mcp_token_on_the_rest_audience() => _restAudience.IsValid.ShouldBeFalse();
    [Fact] void should_refuse_a_rest_token_on_the_mcp_audience() => _restTokenOnMcp.IsValid.ShouldBeFalse();

    static string Issue(RealmKey signer, string audience) => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
    {
        Issuer = LocalIssuer,
        Audience = audience,
        SigningCredentials = new SigningCredentials(new RsaSecurityKey(signer.Certificate.GetRSAPrivateKey()) { KeyId = signer.Kid }, SecurityAlgorithms.RsaSha256)
    });

    static TokenValidationParameters Parameters(string issuer, string audience, SecurityKey key) => new()
    {
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = key
    };
}

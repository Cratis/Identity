// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cratis.Identity.Configuration;
using Cratis.Identity.Keys;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_a_deployed_resource_validates_a_local_token : given.a_development_profile
{
    const string DeployedIssuer = "https://id.example.com/";
    const string Audience = "direct-api";

    SecretStoreKeys _keys;
    string _token;
    TokenValidationResult _deployed;
    TokenValidationResult _wrongIssuerOnly;
    TokenValidationResult _local;

    async Task Because()
    {
        Apply();
        _keys = new SecretStoreKeys(new SecretStoreOptions { Path = _storeDirectory }, TimeProvider.System, NullLogger<SecretStoreKeys>.Instance, providesDataProtection: false);
        var signer = _keys.Current.Signer;
        _token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = LocalIssuer,
            Audience = Audience,
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(signer.Certificate.GetRSAPrivateKey()) { KeyId = signer.Kid }, SecurityAlgorithms.RsaSha256)
        });

        using var deployedKey = RSA.Create(2048);
        var handler = new JsonWebTokenHandler();
        _deployed = await handler.ValidateTokenAsync(_token, Parameters(DeployedIssuer, new RsaSecurityKey(deployedKey.ExportParameters(false))));
        _wrongIssuerOnly = await handler.ValidateTokenAsync(_token, Parameters(DeployedIssuer, new RsaSecurityKey(signer.Certificate.GetRSAPublicKey()) { KeyId = signer.Kid }));
        _local = await handler.ValidateTokenAsync(_token, Parameters(LocalIssuer, new RsaSecurityKey(signer.Certificate.GetRSAPublicKey()) { KeyId = signer.Kid }));
    }

    void Destroy() => _keys?.Dispose();

    [Fact] void should_reject_it_with_the_deployed_issuer_and_keys() => _deployed.IsValid.ShouldBeFalse();
    [Fact] void should_reject_it_with_the_deployed_issuer_even_when_the_key_matches() => _wrongIssuerOnly.IsValid.ShouldBeFalse();
    [Fact] void should_accept_it_only_against_the_local_issuer() => _local.IsValid.ShouldBeTrue();

    static TokenValidationParameters Parameters(string issuer, SecurityKey key) => new()
    {
        ValidIssuer = issuer,
        ValidAudience = Audience,
        IssuerSigningKey = key
    };
}

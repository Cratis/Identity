// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_IdentityTokenSigning;

public class when_generating_an_identity_token_with_ec_first : given.a_token_generation
{
    Task Because() => Generate();

    [Fact] void should_select_rs256() => _jwt.Alg.ShouldEqual("RS256");
    [Fact] void should_publish_the_rsa_kid() => _jwt.Kid.ShouldEqual("rsa-spec");
    [Fact] void should_be_a_signed_jwt() => _jwt.Typ.ShouldEqual("JWT");
    [Fact] void should_validate_with_the_rsa_key() => _signatureValid.ShouldBeTrue();
    [Fact] void should_encode_aud_as_an_array() => _payload.GetProperty("aud").ValueKind.ShouldEqual(System.Text.Json.JsonValueKind.Array);
    [Fact] void should_bind_only_to_the_client() => _payload.GetProperty("aud").EnumerateArray().Select(value => value.GetString()).ShouldContainOnly(ClientId);
    [Fact] void should_encode_auth_time_as_an_integer() => _payload.GetProperty("auth_time").GetInt64().ShouldEqual(AuthTime.ToUnixTimeSeconds());
    [Fact] void should_keep_the_nonce() => _payload.GetProperty("nonce").GetString().ShouldEqual("example-nonce");
    [Fact] void should_mint_a_fresh_token_identifier() => _payload.GetProperty("jti").GetString().ShouldNotEqual("previous-token");
    [Fact] void should_apply_the_five_minute_default() => (_payload.GetProperty("exp").GetInt64() - _payload.GetProperty("iat").GetInt64()).ShouldEqual(300L);
    [Fact] void should_emit_only_the_contracted_claims() => _payload.EnumerateObject().Select(property => property.Name).ShouldContainOnly("iss", "sub", "aud", "exp", "iat", "azp", "auth_time", "nonce", "sid", "jti", "name", "preferred_username");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_Oidc;

[Requirement(Requirements.OidcIdTokenClaims)]
[Collection(OidcCollection.Name)]
public class when_reading_the_id_token_claims(OidcIdentityHost host) : an_oidc_login(host)
{
    [Fact] void should_use_the_exact_issuer() => Check(() => _identity.GetProperty("iss").GetString().ShouldEqual(IdentityHost.Issuer));
    [Fact] void should_use_a_single_client_audience_array() => Check(() => _identity.GetProperty("aud").EnumerateArray().Select(value => value.GetString()).ShouldContainOnly(SpecClientDescriptors.OidcWeb));
    [Fact] void should_bind_the_authorized_party() => Check(() => _identity.GetProperty("azp").GetString().ShouldEqual(SpecClientDescriptors.OidcWeb));
    [Fact] void should_use_integer_auth_time() => Check(() => _identity.GetProperty("auth_time").GetInt64().ShouldBeGreaterThan(0L));
    [Fact] void should_use_a_five_minute_lifetime() => Check(() => (_identity.GetProperty("exp").GetInt64() - _identity.GetProperty("iat").GetInt64()).ShouldEqual(300L));
    [Fact] void should_have_exactly_the_profile_contract() => Check(() => _identity.EnumerateObject().Select(property => property.Name).Where(name => name != "sid").ShouldContainOnly("iss", "sub", "aud", "exp", "iat", "auth_time", "nonce", "azp", "jti", "at_hash", "name", "preferred_username"));
    [Fact] void should_bind_the_access_token_hash() => Check(() => _identity.GetProperty("at_hash").GetString().ShouldEqual(OidcTokens.AccessTokenHash(_accessToken)));

    static void Check(Action assertion) => Conformance.Conformance.Expect(Requirements.OidcIdTokenClaims, assertion);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.IdentityModel.JsonWebTokens;

namespace Cratis.Identity.Integration.for_Oidc;

[Requirement(Requirements.OidcIdTokenSigned)]
[Collection(OidcCollection.Name)]
public class when_verifying_the_id_token_signature(OidcIdentityHost host) : an_oidc_login(host)
{
    bool _valid;
    JsonWebToken _jwt;
    string[] _publishedKids;

    async Task Because()
    {
        _valid = (await OidcTokens.Validate(_browser.Client, _idToken)).IsValid;
        _jwt = new(_idToken);
        var jwks = JsonElement.Parse(await _browser.Client.GetStringAsync("/.well-known/jwks"));
        _publishedKids = jwks.GetProperty("keys").EnumerateArray().Select(key => key.GetProperty("kid").GetString()!).ToArray();
    }

    [Fact] void should_validate_using_public_discovery_keys() => Check(() => _valid.ShouldBeTrue());
    [Fact] void should_use_rs256() => Check(() => _jwt.Alg.ShouldEqual("RS256"));
    [Fact] void should_use_jwt_not_an_access_token_type() => Check(() => _jwt.Typ.ShouldEqual("JWT"));
    [Fact] void should_name_a_published_key() => Check(() => _publishedKids.ShouldContain(_jwt.Kid));

    static void Check(Action assertion) => Conformance.Conformance.Expect(Requirements.OidcIdTokenSigned, assertion);
}

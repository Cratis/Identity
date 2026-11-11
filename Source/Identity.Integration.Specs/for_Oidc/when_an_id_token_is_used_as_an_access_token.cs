// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.Integration.for_Oidc;

[Requirement(Requirements.OidcIdTokenNotAccess)]
[Collection(OidcCollection.Name)]
public class when_an_id_token_is_used_as_an_access_token(OidcIdentityHost host) : an_oidc_login(host)
{
    HttpStatusCode _userInfoStatus;
    JsonElement _introspection;
    HttpStatusCode _introspectionStatus;
    bool _resourceValidationRejected;

    async Task Because()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/connect/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _idToken);
        using var response = await _browser.Client.SendAsync(request);
        _userInfoStatus = response.StatusCode;
        var introspection = await _browser.Introspect(_idToken);
        _introspection = introspection.Body;
        _introspectionStatus = introspection.Status;
        var keys = new JsonWebKeySet(await _browser.Client.GetStringAsync("/.well-known/jwks")).GetSigningKeys();
        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(_idToken, new TokenValidationParameters
        {
            ValidIssuer = IdentityHost.Issuer,
            ValidAudience = IdentityHost.DirectMcpAudience,
            IssuerSigningKeys = keys,
            ValidTypes = ["at+jwt"]
        });
        _resourceValidationRejected = !validation.IsValid;
    }

    [Fact] void should_refuse_it_at_userinfo() => Check(() => _userInfoStatus.ShouldEqual(HttpStatusCode.Unauthorized));
    [Fact]
    void should_reject_the_unsupported_token_type_at_introspection() => Check(() =>
    {
        _introspectionStatus.ShouldEqual(HttpStatusCode.BadRequest);
        _introspection.GetProperty("error").GetString().ShouldEqual("unsupported_token_type");
        _introspection.TryGetProperty("active", out _).ShouldBeFalse();
    });
    [Fact] void should_not_return_a_cid_alias() => Check(() => _introspection.TryGetProperty("cid", out _).ShouldBeFalse());
    [Fact] void should_not_return_a_credential_alias() => Check(() => _introspection.TryGetProperty("credential_id", out _).ShouldBeFalse());
    [Fact] void should_fail_resource_audience_and_type_validation() => Check(() => _resourceValidationRejected.ShouldBeTrue());

    static void Check(Action assertion) => Conformance.Conformance.Expect(Requirements.OidcIdTokenNotAccess, assertion);
}

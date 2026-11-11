// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_Oidc;

[Collection(OidcCollection.Name)]
public class when_refreshing_an_identity_only_grant(OidcIdentityHost host) : an_oidc_login(host)
{
    protected override string Scope => "openid profile offline_access";
    HttpStatusCode _status;
    HttpStatusCode _reuse;
    HttpStatusCode _successorAfterReuse;
    JsonElement _refreshed;
    JsonElement _renewedIdentity;

    async Task Because()
    {
        var original = _tokens.GetProperty("refresh_token").GetString();
        var result = await _browser.PostForm("/connect/token", [new("grant_type", "refresh_token"), new("refresh_token", original), new("nonce", "must-be-ignored")], (ClientId, SpecClientDescriptors.OidcClientSecret));
        _status = result.Status;
        _refreshed = result.Body;
        _renewedIdentity = OidcTokens.Payload(_refreshed.GetProperty("id_token").GetString());
        _reuse = (await OidcRequests.Refresh(_browser, original)).Status;
        _successorAfterReuse = (await OidcRequests.Refresh(_browser, _refreshed.GetProperty("refresh_token").GetString())).Status;
    }

    [Fact] void should_issue_a_new_token_response() => _status.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_keep_the_original_authentication_time() => _renewedIdentity.GetProperty("auth_time").GetInt64().ShouldEqual(_identity.GetProperty("auth_time").GetInt64());
    [Fact] void should_keep_the_subject() => _renewedIdentity.GetProperty("sub").GetString().ShouldEqual(_identity.GetProperty("sub").GetString());
    [Fact] void should_mint_a_fresh_token_identifier() => _renewedIdentity.GetProperty("jti").GetString().ShouldNotEqual(_identity.GetProperty("jti").GetString());
    [Fact] void should_omit_nonce_instead_of_accepting_the_token_parameter() => _renewedIdentity.TryGetProperty("nonce", out _).ShouldBeFalse();
    [Fact] void should_hash_the_new_access_token() => _renewedIdentity.GetProperty("at_hash").GetString().ShouldEqual(OidcTokens.AccessTokenHash(_refreshed.GetProperty("access_token").GetString()));
    [Fact] void should_not_reuse_the_old_access_token_hash() => _renewedIdentity.GetProperty("at_hash").GetString().ShouldNotEqual(_identity.GetProperty("at_hash").GetString());
    [Fact] void should_rotate_the_refresh_token() => _refreshed.GetProperty("refresh_token").GetString().ShouldNotEqual(_tokens.GetProperty("refresh_token").GetString());
    [Fact] void should_refuse_reuse() => _reuse.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_fence_the_successor_after_reuse() => _successorAfterReuse.ShouldEqual(HttpStatusCode.BadRequest);
}

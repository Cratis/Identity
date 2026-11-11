// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_IdentityTokenSigning;

public class when_generating_a_refresh_identity_token : given.a_token_generation
{
    protected override string GrantType => "refresh_token";

    Task Because() => Generate();

    [Fact] void should_omit_nonce_on_refresh() => _payload.TryGetProperty("nonce", out _).ShouldBeFalse();
    [Fact] void should_preserve_the_original_authentication_time() => _payload.GetProperty("auth_time").GetInt64().ShouldEqual(AuthTime.ToUnixTimeSeconds());
    [Fact] void should_preserve_the_session_identifier() => _payload.GetProperty("sid").GetString().ShouldEqual(SessionId);
    [Fact] void should_emit_only_the_refresh_contract() => _payload.EnumerateObject().Select(property => property.Name).ShouldContainOnly("iss", "sub", "aud", "exp", "iat", "azp", "auth_time", "sid", "jti", "name", "preferred_username");
}

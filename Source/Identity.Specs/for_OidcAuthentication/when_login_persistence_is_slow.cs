// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using OpenIddict.Abstractions;

namespace Cratis.Identity.for_OidcAuthentication;

public class when_login_persistence_is_slow : Specification
{
    readonly DateTimeOffset _loginTime = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);
    OpenIddictRequest _continuation;

    void Because() => _continuation = new OpenIddictRequest(AuthorizationParameters.AfterLogin(
    [
        new("client_id", "oidc-web"),
        new("scope", "openid profile"),
        new("nonce", "request-nonce"),
        new("state", "client-state"),
        new("code_challenge", "request-challenge"),
        new("max_age", "0"),
        new("prompt", "login consent")
    ]));

    [Fact] void should_require_login_for_the_original_request() =>
        OidcAuthentication.IsFresh(new OpenIddictRequest { Scope = "openid", MaxAge = 0 }, _loginTime, _loginTime).ShouldBeFalse();
    [Fact] void should_not_loop_when_persistence_crosses_a_second_boundary() =>
        OidcAuthentication.IsFresh(_continuation, _loginTime, _loginTime.AddSeconds(10)).ShouldBeTrue();
    [Fact] void should_remove_the_satisfied_age_requirement() => _continuation.MaxAge.ShouldBeNull();
    [Fact] void should_preserve_the_nonce() => _continuation.Nonce.ShouldEqual("request-nonce");
    [Fact] void should_preserve_the_client_state() => _continuation.State.ShouldEqual("client-state");
    [Fact] void should_preserve_pkce() => _continuation.CodeChallenge.ShouldEqual("request-challenge");
    [Fact] void should_preserve_remaining_prompts() => _continuation.Prompt.ShouldEqual("consent");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_SessionPrincipal;

public class when_creating_browser_sessions : Specification
{
    ClaimsPrincipal _first;
    ClaimsPrincipal _second;
    ClaimsPrincipal _legacy;
    readonly DateTimeOffset _authTime = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);

    void Because()
    {
        var account = new Account(new("7d9d5c9e-0000-4000-8000-000000000001"), "Member", null, "42", "member", AccountStatus.Active, []);
        _first = SessionPrincipal.For(account, "test", _authTime);
        _second = SessionPrincipal.For(account, "test", _authTime);
        _legacy = new(new ClaimsIdentity([new Claim(Claims.Subject, account.Id.Value)], "test"));
    }

    [Fact] void should_create_an_opaque_session_identifier() => Guid.TryParseExact(SessionPrincipal.SessionId(_first), "N", out _).ShouldBeTrue();
    [Fact] void should_rotate_the_identifier_on_each_sign_in() => (SessionPrincipal.SessionId(_first) == SessionPrincipal.SessionId(_second)).ShouldBeFalse();
    [Fact] void should_keep_the_account_subject() => SessionPrincipal.AccountId(_first).ShouldEqual("7d9d5c9e-0000-4000-8000-000000000001");
    [Fact] void should_keep_the_upstream_authentication_time() => SessionPrincipal.AuthTime(_first).ShouldEqual(_authTime);
    [Fact] void should_not_give_the_identifier_a_token_destination() => _first.FindFirst("sid").GetDestinations().ShouldBeEmpty();
    [Fact] void should_not_invent_an_identifier_for_an_old_cookie() => SessionPrincipal.SessionId(_legacy).ShouldBeNull();
}

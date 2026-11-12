// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.given;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_SignedInAccounts;

public class when_authentication_age_is_requested : Specification
{
    SignedInAccount _stale;
    SignedInAccount _fresh;
    SignedInAccount _unbounded;
    SignedInAccount _zero;
    SignedInAccount _nonInteractive;
    SignedInAccount _legacy;
    int _staleReads;
    int _legacyReads;
    DateTimeOffset _authTime;
    string _sessionId;

    async Task Because()
    {
        var time = new FixedTimeProvider(DateTimeOffset.FromUnixTimeSeconds(1_700_000_000));
        _authTime = time.Now.AddMinutes(-5);
        var account = new Account(new("7d9d5c9e-0000-4000-8000-000000000001"), "Member", null, "42", "member", AccountStatus.Active, []);
        var accounts = Substitute.For<IAccountReader>();
        accounts.FindActive(Arg.Any<AccountSubject>(), Arg.Any<CancellationToken>()).Returns(account);
        var principal = SessionPrincipal.For(account, "test", _authTime);
        _sessionId = SessionPrincipal.SessionId(principal);
        var authentication = Substitute.For<IAuthenticationService>();
        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), Arg.Any<string>()).Returns(AuthenticateResult.Success(new(principal, "test")));
        await using var services = new ServiceCollection().AddSingleton(authentication).BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        var finder = new SignedInAccounts(accounts, Options.Create(new UpstreamLoginOptions()), time);
        _stale = await finder.Find(context, new() { MaxAge = 60 });
        _staleReads = accounts.ReceivedCalls().Count();
        _fresh = await finder.Find(context, new() { MaxAge = 600 });
        _unbounded = await finder.Find(context, new());
        _zero = await finder.Find(context, new() { MaxAge = 0 });
        _nonInteractive = await finder.Find(context, new() { MaxAge = 60, Prompt = PromptValues.None });
        var legacy = new ClaimsPrincipal(new ClaimsIdentity(principal.Claims.Where(_ => _.Type != "sid"), "test"));
        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), Arg.Any<string>()).Returns(AuthenticateResult.Success(new(legacy, "test")));
        accounts.ClearReceivedCalls();
        _legacy = await finder.Find(context, new());
        _legacyReads = accounts.ReceivedCalls().Count();
    }

    [Fact] void should_require_reauthentication_for_a_stale_cookie() => _stale.ShouldBeNull();
    [Fact] void should_not_look_up_the_account_of_a_stale_cookie() => _staleReads.ShouldEqual(0);
    [Fact] void should_accept_fresh_authentication() => _fresh.ShouldNotBeNull();
    [Fact] void should_not_refresh_authentication_time_when_reusing_a_cookie() => _fresh.AuthTime.ShouldEqual(_authTime);
    [Fact] void should_preserve_the_session_identifier() => _fresh.SessionId.ShouldEqual(_sessionId);
    [Fact] void should_preserve_the_behavior_without_max_age() => _unbounded.ShouldNotBeNull();
    [Fact] void should_always_reauthenticate_when_max_age_is_zero() => _zero.ShouldBeNull();
    [Fact] void should_return_no_account_for_a_stale_noninteractive_request() => _nonInteractive.ShouldBeNull();
    [Fact] void should_require_reauthentication_for_a_cookie_without_a_session_identifier() => _legacy.ShouldBeNull();
    [Fact] void should_not_look_up_the_account_of_a_cookie_without_a_session_identifier() => _legacyReads.ShouldEqual(0);
}

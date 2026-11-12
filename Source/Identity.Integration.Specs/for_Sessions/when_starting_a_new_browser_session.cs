// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Cratis.Identity.Integration.for_Sessions;

[Collection(IdentityCollection.Name)]
public class when_starting_a_new_browser_session(IdentityHost host) : Specification
{
    ClaimsPrincipal _first;
    ClaimsPrincipal _roundTrip;
    ClaimsPrincipal _second;
    HttpStatusCode _resumed;

    async Task Because()
    {
        var browser = host.CreateBrowser();
        using var client = browser.Client;
        var options = host.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(IdentitySchemes.Session);
        var parameters = Browser.CliAuthorization(Browser.Pkce().Challenge);
        using var first = await client.GetAsync($"/connect/authorize{QueryString.Create(parameters)}");
        var ticket = ReadCookie(first, options);
        _first = ticket.Principal;
        _roundTrip = options.TicketDataFormat.Unprotect(options.TicketDataFormat.Protect(ticket)).Principal;
        using var resumed = await client.GetAsync(first.Headers.Location);
        _resumed = resumed.StatusCode;
        parameters.Add(new("prompt", "login"));
        using var second = await client.GetAsync($"/connect/authorize{QueryString.Create(parameters)}");
        _second = ReadCookie(second, options).Principal;
    }

    static AuthenticationTicket ReadCookie(HttpResponseMessage response, CookieAuthenticationOptions options)
    {
        var cookie = response.Headers.GetValues("Set-Cookie").Select(_ => SetCookieHeaderValue.Parse(_)).Single(_ => _.Name == options.Cookie.Name);
        return options.TicketDataFormat.Unprotect(cookie.Value.Value) ?? throw new InvalidOperationException("Expected a valid browser session ticket.");
    }

    [Fact] void should_put_a_session_identifier_in_the_real_cookie() => string.IsNullOrEmpty(SessionPrincipal.SessionId(_first)).ShouldBeFalse();
    [Fact] void should_preserve_it_through_cookie_protection() => SessionPrincipal.SessionId(_roundTrip).ShouldEqual(SessionPrincipal.SessionId(_first));
    [Fact] void should_preserve_authentication_time_through_cookie_protection() => SessionPrincipal.AuthTime(_roundTrip).ShouldEqual(SessionPrincipal.AuthTime(_first));
    [Fact] void should_resume_the_existing_session_at_consent() => _resumed.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_rotate_the_identifier_on_a_new_upstream_sign_in() => (SessionPrincipal.SessionId(_second) == SessionPrincipal.SessionId(_first)).ShouldBeFalse();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Cratis.Identity.Integration.for_Sessions;

[Collection(IdentityCollection.Name)]
public class when_a_stale_session_requests_authorization(IdentityHost host) : Specification
{
    string _error;
    string _forcedError;
    bool _reissuedOnError;
    bool _reauthenticated;
    bool _reissuedWithoutMaxAge;

    async Task Because()
    {
        var browser = host.CreateBrowser();
        using var login = browser.Client;
        var parameters = Browser.CliAuthorization(Browser.Pkce().Challenge);
        using var first = await login.GetAsync($"/connect/authorize{QueryString.Create(parameters)}");
        var options = host.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(IdentitySchemes.Session);
        var cookie = first.Headers.GetValues("Set-Cookie").Select(_ => SetCookieHeaderValue.Parse(_)).Single(_ => _.Name == options.Cookie.Name);
        var ticket = options.TicketDataFormat.Unprotect(cookie.Value.Value);
        var identity = (ClaimsIdentity)ticket.Principal.Identity;
        identity.RemoveClaim(identity.FindFirst("auth_time"));
        identity.AddClaim(new("auth_time", "10000000", ClaimValueTypes.Integer64));
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false, BaseAddress = new(IdentityHost.Issuer) });
        client.DefaultRequestHeaders.Add(FakeUpstream.UserHeader, FakeUpstream.DefaultUser);
        client.DefaultRequestHeaders.Add("Cookie", $"{options.Cookie.Name}={options.TicketDataFormat.Protect(ticket)}");
        using var unbounded = await client.GetAsync($"/connect/authorize{QueryString.Create(parameters)}");
        _reissuedWithoutMaxAge = SetsSessionCookie(unbounded, options.Cookie.Name);
        using var silent = await client.GetAsync($"/connect/authorize{QueryString.Create(parameters.Concat([new("max_age", "60"), new("prompt", "none")]))}");
        _error = QueryHelpers.ParseQuery(silent.Headers.Location.Query)["error"].ToString();
        _reissuedOnError = SetsSessionCookie(silent, options.Cookie.Name);
        using var forced = await client.GetAsync($"/connect/authorize{QueryString.Create(parameters.Append(new("max_age", "0")))}");
        _forcedError = QueryHelpers.ParseQuery(forced.Headers.Location.Query)["error"].ToString();
        _reauthenticated = SetsSessionCookie(forced, options.Cookie.Name);
    }

    static bool SetsSessionCookie(HttpResponseMessage response, string cookieName) =>
        response.Headers.TryGetValues("Set-Cookie", out var values) && values.Any(_ => SetCookieHeaderValue.Parse(_).Name == cookieName);

    [Fact] void should_refuse_max_age_as_an_invalid_request_even_when_prompt_is_none() => _error.ShouldEqual("invalid_request");
    [Fact] void should_not_refresh_the_cookie_when_max_age_is_refused() => _reissuedOnError.ShouldBeFalse();
    [Fact] void should_refuse_max_age_zero_as_an_invalid_request() => _forcedError.ShouldEqual("invalid_request");
    [Fact] void should_not_issue_a_session_cookie_when_max_age_zero_is_refused() => _reauthenticated.ShouldBeFalse();
    [Fact] void should_not_reauthenticate_a_legacy_request_without_max_age() => _reissuedWithoutMaxAge.ShouldBeFalse();
}

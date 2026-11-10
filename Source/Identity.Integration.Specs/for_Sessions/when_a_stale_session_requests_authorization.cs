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
        _reissuedWithoutMaxAge = unbounded.Headers.Contains("Set-Cookie");
        using var silent = await client.GetAsync($"/connect/authorize{QueryString.Create(parameters.Concat([new("max_age", "60"), new("prompt", "none")]))}");
        _error = QueryHelpers.ParseQuery(silent.Headers.Location.Query)["error"].ToString();
        _reissuedOnError = silent.Headers.Contains("Set-Cookie");
        using var forced = await client.GetAsync($"/connect/authorize{QueryString.Create(parameters.Append(new("max_age", "0")))}");
        _reauthenticated = forced.Headers.Contains("Set-Cookie");
    }

    [Fact] void should_return_login_required_without_interactive_login_for_a_stale_silent_request() => _error.ShouldEqual("login_required");
    [Fact] void should_not_refresh_the_cookie_on_a_silent_error() => _reissuedOnError.ShouldBeFalse();
    [Fact] void should_reauthenticate_when_max_age_is_zero() => _reauthenticated.ShouldBeTrue();
    [Fact] void should_not_reauthenticate_a_legacy_request_without_max_age() => _reissuedWithoutMaxAge.ShouldBeFalse();
}

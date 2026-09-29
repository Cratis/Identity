// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Hosting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_IdentityServiceCollectionExtensions;

public class when_adding_the_session_outside_development : Specification
{
    ServiceCollection _services;
    CookieAuthenticationOptions _session;
    AntiforgeryOptions _antiforgery;

    void Establish()
    {
        _services = new ServiceCollection();
        _services.AddLogging();
        _services.AddDataProtection();
    }

    void Because()
    {
        _services.AddSessionAndUpstreamLogin(new HostingEnvironment { EnvironmentName = Environments.Production });
        var provider = _services.BuildServiceProvider();
        _session = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(IdentitySchemes.Session);
        _antiforgery = provider.GetRequiredService<IOptions<AntiforgeryOptions>>().Value;
    }

    [Fact] void should_always_mark_the_session_cookie_secure() => _session.Cookie.SecurePolicy.ShouldEqual(CookieSecurePolicy.Always);
    [Fact] void should_make_the_session_cookie_http_only() => _session.Cookie.HttpOnly.ShouldBeTrue();
    [Fact] void should_send_the_session_cookie_on_top_level_navigation_only() => _session.Cookie.SameSite.ShouldEqual(SameSiteMode.Lax);
    [Fact] void should_always_mark_the_antiforgery_cookie_secure() => _antiforgery.Cookie.SecurePolicy.ShouldEqual(CookieSecurePolicy.Always);
    [Fact] void should_make_the_antiforgery_cookie_http_only() => _antiforgery.Cookie.HttpOnly.ShouldBeTrue();
    [Fact] void should_keep_the_antiforgery_cookie_same_site_strict() => _antiforgery.Cookie.SameSite.ShouldEqual(SameSiteMode.Strict);
}

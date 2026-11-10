// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Upstream;

/// <summary>Registers the Identity session cookie, antiforgery and upstream login.</summary>
internal static class UpstreamServiceCollectionExtensions
{
    /// <summary>Adds the session and upstream login.</summary>
    /// <param name="services">The services.</param>
    /// <param name="environment">The host environment.</param>
    internal static void AddSessionAndUpstreamLogin(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddScoped<SignedInAccounts>();
        services.AddOptions<UpstreamLoginOptions>();
        services.AddAntiforgery(options =>
        {
            options.FormFieldName = ConsentForm.AntiforgeryField;
            options.Cookie.Name = "cratis-identity-antiforgery";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        });

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = IdentitySchemes.Session;

                // The device authorization endpoint signs in a deliberately unauthenticated principal, as OpenIddict requires.
                options.RequireAuthenticatedSignIn = false;
            })
            .AddCookie(IdentitySchemes.Session, options =>
            {
                options.Cookie.Name = "cratis-identity-session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
                options.SlidingExpiration = false;
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
            })
            .AddGitHubUpstream();

        services.AddOptions<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>(IdentitySchemes.Session)
            .Configure<IOptions<IdentityOptions>>((cookie, identity) => cookie.ExpireTimeSpan = identity.Value.SessionLifetime);
    }
}

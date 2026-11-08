// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace Cratis.Identity.Integration.for_Sessions;

[Collection(IdentityCollection.Name)]
public partial class when_signing_out(IdentityHost host) : IAsyncLifetime
{
    HttpStatusCode _withoutAntiforgery;
    HttpStatusCode _signOut;
    string _authorizationError = string.Empty;
    string _sessionCookie = string.Empty;
    string _antiforgeryCookie = string.Empty;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (_, challenge) = Browser.Pkce();
        using var login = await browser.Client.GetAsync($"/connect/authorize{Microsoft.AspNetCore.Http.QueryString.Create(Browser.CliAuthorization(challenge))}");
        _sessionCookie = login.Headers.GetValues("Set-Cookie").Single(_ => _.StartsWith("cratis-identity-session=", StringComparison.Ordinal));
        using var logoutPage = await browser.Client.GetAsync("/logout");
        _antiforgeryCookie = logoutPage.Headers.GetValues("Set-Cookie").Single(_ => _.StartsWith("cratis-identity-antiforgery=", StringComparison.Ordinal));
        var html = await logoutPage.Content.ReadAsStringAsync();
        var token = AntiforgeryField.Match(html).Groups["token"].Value;
        (token.Length > 0).ShouldBeTrue();

        using var invalid = await browser.Client.PostAsync("/logout", new FormUrlEncodedContent([]));
        _withoutAntiforgery = invalid.StatusCode;
        using var signedOut = await browser.Client.PostAsync("/logout", new FormUrlEncodedContent([new("__consent_antiforgery", WebUtility.HtmlDecode(token))]));
        _signOut = signedOut.StatusCode;

        var parameters = Browser.CliAuthorization(challenge).Append(new("prompt", "none"));
        using var response = await browser.Authorize(parameters);
        _authorizationError = QueryHelpers.ParseQuery(response.Headers.Location.Query)["error"].ToString();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_reject_a_cross_site_post() => _withoutAntiforgery.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_sign_out_with_the_antiforgery_token() => _signOut.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_require_a_new_login_after_sign_out() => _authorizationError.ShouldEqual("login_required");
    [Fact] void should_secure_the_session_cookie() => (_sessionCookie.Contains("httponly", StringComparison.OrdinalIgnoreCase) && _sessionCookie.Contains("secure", StringComparison.OrdinalIgnoreCase) && _sessionCookie.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
    [Fact] void should_secure_the_antiforgery_cookie() => (_antiforgeryCookie.Contains("httponly", StringComparison.OrdinalIgnoreCase) && _antiforgeryCookie.Contains("secure", StringComparison.OrdinalIgnoreCase) && _antiforgeryCookie.Contains("samesite=strict", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();

    [GeneratedRegex("name=\"__consent_antiforgery\" value=\"(?<token>[^\"]+)\"", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex AntiforgeryField { get; }
}

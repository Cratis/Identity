// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.WebUtilities;

namespace Cratis.Identity.Integration.for_CliLogin;

[Collection(IdentityCollection.Name)]
public class when_logging_in_on_an_ipv6_loopback_port(IdentityHost host) : IAsyncLifetime
{
    const string RedirectUri = "http://[::1]:52673/callback";
    Uri _callback;
    HttpStatusCode _tokenStatus;

    public async Task InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var parameters = Browser.CliAuthorization(challenge);
        var redirect = parameters.FindIndex(_ => _.Key == "redirect_uri");
        parameters[redirect] = new("redirect_uri", RedirectUri);

        using var page = await browser.Authorize(parameters);
        using var consent = await browser.Consent(await page.Content.ReadAsStringAsync());
        _callback = consent.Headers.Location!;
        var code = QueryHelpers.ParseQuery(_callback.Query)["code"].ToString();
        (_tokenStatus, _) = await browser.PostForm("/connect/token",
        [
            new("grant_type", "authorization_code"),
            new("client_id", Browser.ClientId),
            new("code", code),
            new("redirect_uri", RedirectUri),
            new("code_verifier", verifier),
            new("resource", IdentityHost.DirectMcp)
        ]);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact] void should_return_to_the_ipv6_loopback_port() => _callback.GetLeftPart(UriPartial.Path).ShouldEqual(RedirectUri);
    [Fact] void should_redeem_the_code_with_the_exact_redirect_uri() => _tokenStatus.ShouldEqual(HttpStatusCode.OK);
}

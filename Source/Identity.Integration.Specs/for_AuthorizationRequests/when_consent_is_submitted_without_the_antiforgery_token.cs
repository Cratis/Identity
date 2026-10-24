// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_AuthorizationRequests;

[Collection(IdentityCollection.Name)]
public class when_consent_is_submitted_without_the_antiforgery_token(IdentityHost host) : IAsyncLifetime
{
    HttpResponseMessage _response;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (_, challenge) = Browser.Pkce();
        using var page = await browser.Authorize(Browser.CliAuthorization(challenge));
        _response = await browser.Consent(await page.Content.ReadAsStringAsync(), includeAntiforgery: false);
    }

    public ValueTask DisposeAsync()
    {
        _response.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact] void should_refuse_the_submission() => _response.StatusCode.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_not_redirect_with_a_code() => _response.Headers.Location.ShouldBeNull();
}

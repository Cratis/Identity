// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_AuthorizationRequests;

[Collection(IdentityCollection.Name)]
public class when_a_tenant_outside_the_selectable_list_is_submitted(IdentityHost host) : IAsyncLifetime
{
    HttpResponseMessage _response;
    string _page;

    public async Task InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (_, challenge) = Browser.Pkce();
        using var page = await browser.Authorize(Browser.CliAuthorization(challenge));
        _response = await browser.Consent(await page.Content.ReadAsStringAsync(), tenant: "someone-elses-tenant");
        _page = await _response.Content.ReadAsStringAsync();
    }

    public Task DisposeAsync()
    {
        _response.Dispose();
        return Task.CompletedTask;
    }

    [Fact] void should_not_redirect_with_a_code() => _response.Headers.Location.ShouldBeNull();
    [Fact] void should_ask_again() => _page.Contains("Select one of the listed tenants.", StringComparison.Ordinal).ShouldBeTrue();
}

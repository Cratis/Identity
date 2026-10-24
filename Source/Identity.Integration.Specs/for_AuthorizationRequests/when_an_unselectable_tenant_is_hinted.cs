// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_AuthorizationRequests;

[Collection(IdentityCollection.Name)]
public class when_an_unselectable_tenant_is_hinted(IdentityHost host) : IAsyncLifetime
{
    string _page;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (_, challenge) = Browser.Pkce();
        var parameters = Browser.CliAuthorization(challenge);
        parameters.Add(new("tenant", "someone-elses-tenant"));

        using var page = await browser.Authorize(parameters);
        _page = await page.Content.ReadAsStringAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_not_select_a_tenant_without_membership() => _page.Contains(" checked required", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_still_offer_selectable_tenants() => _page.Contains($"value=\"{IdentityHost.MemberTenant}\" required", StringComparison.Ordinal).ShouldBeTrue();
}

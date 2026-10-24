// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.WebUtilities;

namespace Cratis.Identity.Integration.for_AuthorizationRequests;

[Collection(IdentityCollection.Name)]
public class when_a_selectable_tenant_is_hinted(IdentityHost host) : IAsyncLifetime
{
    string _page;
    string _selectedTenant;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (verifier, challenge) = Browser.Pkce();
        var parameters = Browser.CliAuthorization(challenge);
        parameters.Add(new("tenant", IdentityHost.OtherMemberTenant));

        using var page = await browser.Authorize(parameters);
        _page = await page.Content.ReadAsStringAsync();
        using var redirect = await browser.Consent(_page, tenant: IdentityHost.OtherMemberTenant);
        var code = QueryHelpers.ParseQuery(redirect.Headers.Location!.Query)["code"].ToString();
        var (_, tokens) = await browser.RedeemCode(code, verifier);
        var token = await AccessTokens.Validate(browser, tokens.GetProperty("access_token").GetString()!);
        _selectedTenant = (string)token.Claims["tid"];
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_preselect_the_hinted_tenant() => _page.Contains($"value=\"{IdentityHost.OtherMemberTenant}\" checked", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_still_show_the_other_selectable_tenant() => _page.Contains($"value=\"{IdentityHost.MemberTenant}\" required", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_bind_the_grant_to_the_tenant_chosen_on_consent() => _selectedTenant.ShouldEqual(IdentityHost.OtherMemberTenant);
}

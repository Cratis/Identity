// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Identity.Tenants;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Identity.Integration.for_TenantsCallback;

[Collection(IdentityCollection.Name)]
public partial class when_consent_lists_the_callback_tenants(IdentityHost host) : IAsyncLifetime
{
    string _page;
    TokenValidationResult _token;

    [GeneratedRegex("<option value=\"(?<id>[^\"]+)\"", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex TenantOptions { get; }

    public async ValueTask InitializeAsync()
    {
        host.TenantsCallback.List("501", new SelectableTenant("callback-one", "Callback One"), new SelectableTenant("callback-two", "Callback Two"));
        var browser = host.CreateBrowser("501:callbackmember");
        var (verifier, challenge) = Browser.Pkce();
        using var page = await browser.Authorize(Browser.CliAuthorization(challenge, ["https://stage.example/api"], "stage:read offline_access"));
        _page = await page.Content.ReadAsStringAsync();
        using var redirect = await browser.Consent(_page, tenant: "callback-two");
        var code = QueryHelpers.ParseQuery(redirect.Headers.Location.Query)["code"].ToString();
        var (_, tokens) = await browser.RedeemCode(code, verifier, "https://stage.example/api");
        _token = await AccessTokens.Validate(browser, tokens.GetProperty("access_token").GetString(), "stage-api");
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_list_exactly_the_callback_tenants() => TenantOptions.Matches(_page).Select(_ => _.Groups["id"].Value).ShouldContainOnly("callback-one", "callback-two");
    [Fact] void should_issue_a_valid_token() => _token.IsValid.ShouldBeTrue();
    [Fact] void should_carry_the_chosen_tenant() => _token.Claims["tid"].ShouldEqual("callback-two");
}

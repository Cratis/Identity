// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Tenants;
using Microsoft.AspNetCore.WebUtilities;

namespace Cratis.Identity.Integration.for_TenantsCallback;

[Collection(IdentityCollection.Name)]
public class when_refreshing_while_the_tenants_callback_is_down(IdentityHost host) : IAsyncLifetime
{
    HttpStatusCode _outageStatus;
    JsonElement _outage;
    HttpStatusCode _recoveredStatus;
    JsonElement _recovered;

    public async ValueTask InitializeAsync()
    {
        host.TenantsCallback.List("503", new SelectableTenant("callback-tenant", "Callback Tenant"));
        var browser = host.CreateBrowser("503:callbackrefresh");
        var (verifier, challenge) = Browser.Pkce();
        using var page = await browser.Authorize(Browser.CliAuthorization(challenge, ["https://stage.example/api"], "stage:read offline_access"));
        using var redirect = await browser.Consent(await page.Content.ReadAsStringAsync(), tenant: "callback-tenant");
        var code = QueryHelpers.ParseQuery(redirect.Headers.Location.Query)["code"].ToString();
        var (_, tokens) = await browser.RedeemCode(code, verifier, "https://stage.example/api");
        var refreshToken = tokens.GetProperty("refresh_token").GetString();
        host.TenantsCallback.Fail("503");
        (_outageStatus, _outage) = await Refresh(browser, refreshToken);
        host.TenantsCallback.List("503", new SelectableTenant("callback-tenant", "Callback Tenant"));
        (_recoveredStatus, _recovered) = await Refresh(browser, refreshToken);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_refuse_during_the_outage() => _outageStatus.ShouldNotEqual(HttpStatusCode.OK);
    [Fact] void should_return_a_retryable_error() => _outage.GetProperty("error").GetString().ShouldEqual("temporarily_unavailable");
    [Fact] void should_not_issue_an_access_token_during_the_outage() => _outage.TryGetProperty("access_token", out _).ShouldBeFalse();
    [Fact] void should_accept_the_same_refresh_token_after_recovery() => _recoveredStatus.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_issue_a_successor_after_recovery() => _recovered.TryGetProperty("refresh_token", out _).ShouldBeTrue();

    static Task<(HttpStatusCode Status, JsonElement Body)> Refresh(Browser browser, string token) => browser.PostForm("/connect/token",
    [
        new("grant_type", "refresh_token"),
        new("client_id", Browser.ClientId),
        new("refresh_token", token),
        new("resource", "https://stage.example/api")
    ]);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.WebUtilities;

namespace Cratis.Identity.Integration.for_AuthorizationRequests;

[Collection(IdentityCollection.Name)]
public class when_the_user_denies_consent(IdentityHost host) : IAsyncLifetime
{
    Uri? _redirect;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (_, challenge) = Browser.Pkce();
        using var page = await browser.Authorize(Browser.CliAuthorization(challenge));
        using var response = await browser.Consent(await page.Content.ReadAsStringAsync(), decision: "deny");
        _redirect = response.Headers.Location;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_return_access_denied() => QueryHelpers.ParseQuery(_redirect!.Query)["error"].ToString().ShouldEqual("access_denied");
    [Fact] void should_not_issue_a_code() => QueryHelpers.ParseQuery(_redirect!.Query).ContainsKey("code").ShouldBeFalse();
}

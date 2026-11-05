// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.WebUtilities;

namespace Cratis.Identity.Integration.for_TenantsCallback;

[Collection(IdentityCollection.Name)]
public class when_the_tenants_callback_is_down(IdentityHost host) : IAsyncLifetime
{
    Uri _callback;

    public async ValueTask InitializeAsync()
    {
        host.TenantsCallback.Fail("502");
        var browser = host.CreateBrowser("502:callbackdown");
        var (_, challenge) = Browser.Pkce();
        using var response = await browser.Authorize(Browser.CliAuthorization(challenge, ["https://stage.example/api"], "stage:read"));
        _callback = response.Headers.Location;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_return_a_retryable_error() => QueryHelpers.ParseQuery(_callback.Query)["error"].ToString().ShouldEqual("temporarily_unavailable");
    [Fact] void should_not_issue_a_code() => QueryHelpers.ParseQuery(_callback.Query).ContainsKey("code").ShouldBeFalse();
}

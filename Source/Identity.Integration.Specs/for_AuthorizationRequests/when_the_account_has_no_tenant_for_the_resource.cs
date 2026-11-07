// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.WebUtilities;

namespace Cratis.Identity.Integration.for_AuthorizationRequests;

[Collection(IdentityCollection.Name)]
public class when_the_account_has_no_tenant_for_the_resource(IdentityHost host) : IAsyncLifetime
{
    Uri? _redirect;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateBrowser("42:stranger");
        var (_, challenge) = Browser.Pkce();
        var parameters = Browser.CliAuthorization(challenge);

        using var response = await browser.Authorize(parameters);
        _redirect = response.Headers.Location;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_return_to_the_client_with_an_error() => QueryHelpers.ParseQuery(_redirect.Query)["error"].ToString().ShouldEqual("access_denied");
    [Fact] void should_not_issue_a_code() => QueryHelpers.ParseQuery(_redirect.Query).ContainsKey("code").ShouldBeFalse();
}

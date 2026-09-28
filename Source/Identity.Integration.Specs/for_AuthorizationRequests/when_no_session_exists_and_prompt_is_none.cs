// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.WebUtilities;

namespace Cratis.Identity.Integration.for_AuthorizationRequests;

[Collection(IdentityCollection.Name)]
public class when_no_session_exists_and_prompt_is_none(IdentityHost host) : IAsyncLifetime
{
    Uri? _redirect;

    public async Task InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (_, challenge) = Browser.Pkce();
        var parameters = Browser.CliAuthorization(challenge);
        parameters.Add(new("prompt", "none"));
        using var response = await browser.Authorize(parameters);
        _redirect = response.Headers.Location;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact] void should_return_to_the_client_with_an_error() => QueryHelpers.ParseQuery(_redirect!.Query)["error"].ToString().ShouldEqual("login_required");
    [Fact] void should_not_issue_a_code() => QueryHelpers.ParseQuery(_redirect!.Query).ContainsKey("code").ShouldBeFalse();
}

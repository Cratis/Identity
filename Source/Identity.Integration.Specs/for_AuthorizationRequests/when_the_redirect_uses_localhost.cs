// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_AuthorizationRequests;

[Collection(IdentityCollection.Name)]
public class when_the_redirect_uses_localhost(IdentityHost host) : IAsyncLifetime
{
    HttpResponseMessage _response;

    public async Task InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (_, challenge) = Browser.Pkce();
        var parameters = Browser.CliAuthorization(challenge);
        parameters.RemoveAll(_ => _.Key == "redirect_uri");
        parameters.Add(new("redirect_uri", "http://localhost:53123/callback"));
        _response = await browser.Authorize(parameters);
    }

    public Task DisposeAsync()
    {
        _response.Dispose();
        return Task.CompletedTask;
    }

    [Fact] void should_refuse_the_request() => _response.StatusCode.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_not_redirect_anywhere() => _response.Headers.Location.ShouldBeNull();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_DevelopmentProfile;

[Collection(DevelopmentProfileCollection.Name)]
public class when_a_redirect_uses_localhost_under_the_profile(DevelopmentProfileHost host) : IAsyncLifetime
{
    HttpResponseMessage _profileClientOtherPort;
    HttpResponseMessage _profileClientOtherPath;
    HttpResponseMessage _ordinaryClient;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateLocalBrowser();
        var (_, challenge) = Browser.Pkce();
        _profileClientOtherPort = await browser.Authorize(Parameters(DevelopmentProfileHost.LocalClient, "http://localhost:61234/callback", challenge));
        _profileClientOtherPath = await browser.Authorize(Parameters(DevelopmentProfileHost.LocalClient, "http://localhost:61234/elsewhere", challenge));
        _ordinaryClient = await browser.Authorize(Parameters(Browser.ClientId, DevelopmentProfileHost.LocalRedirect, challenge));
    }

    public ValueTask DisposeAsync()
    {
        _profileClientOtherPort.Dispose();
        _profileClientOtherPath.Dispose();
        _ordinaryClient.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact] void should_accept_a_profile_client_on_an_unregistered_port() => _profileClientOtherPort.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_refuse_a_profile_client_on_an_unregistered_path() => _profileClientOtherPath.StatusCode.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_still_refuse_localhost_for_a_client_that_is_not_a_profile_client() => _ordinaryClient.StatusCode.ShouldEqual(HttpStatusCode.BadRequest);

    static List<KeyValuePair<string, string?>> Parameters(string clientId, string redirect, string challenge)
    {
        var parameters = Browser.CliAuthorization(challenge, [DevelopmentProfileHost.LocalDirectMcp], "direct:mcp:read");
        parameters.RemoveAll(_ => _.Key is "client_id" or "redirect_uri");
        parameters.Add(new("client_id", clientId));
        parameters.Add(new("redirect_uri", redirect));
        return parameters;
    }
}

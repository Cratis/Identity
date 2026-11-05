// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_Host;

[Collection(IdentityCollection.Name)]
public class when_starting_the_host(IdentityHost host) : IAsyncLifetime
{
    HttpResponseMessage _response = null!;
    string _body = string.Empty;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateBrowser();
        _response = await browser.Client.GetAsync("/healthz");
        _body = await _response.Content.ReadAsStringAsync();
    }

    public ValueTask DisposeAsync()
    {
        _response.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact] void should_answer_the_health_check() => (_response.StatusCode == System.Net.HttpStatusCode.OK && _body == "ok").ShouldBeTrue();
    [Fact] void should_send_no_sniff() => _response.Headers.GetValues("X-Content-Type-Options").ShouldContainOnly("nosniff");
    [Fact] void should_refuse_framing() => _response.Headers.GetValues("X-Frame-Options").ShouldContainOnly("DENY");
}

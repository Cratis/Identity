// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_DeviceGrant;

[Requirement(Requirements.DevicePolling)]
[Collection(IdentityCollection.Name)]
public class when_a_device_polls_before_approval(IdentityHost host) : IAsyncLifetime
{
    (HttpStatusCode Status, JsonElement Body) _pending;
    (HttpStatusCode Status, JsonElement Body) _early;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var started = await browser.StartDevice();
        started.Status.ShouldEqual(HttpStatusCode.OK);
        var code = started.Body.GetProperty("device_code").GetString();
        _pending = await browser.PollDevice(code);
        _early = await host.CreateBrowser().PollDevice(code);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_report_pending_not_issue_tokens() => _pending.Body.GetProperty("error").GetString().ShouldEqual("authorization_pending");
    [Fact] void should_reject_early_polling_across_browser_connections() => _early.Body.GetProperty("error").GetString().ShouldEqual("slow_down");
    [Fact] void should_not_return_an_access_token_while_pending() => _pending.Body.TryGetProperty("access_token", out _).ShouldBeFalse();
    [Fact] void should_not_return_an_access_token_when_slowed() => _early.Body.TryGetProperty("access_token", out _).ShouldBeFalse();
}

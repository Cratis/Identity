// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_DeviceGrant;

[Requirement(Requirements.DeviceDenial)]
[Collection(IdentityCollection.Name)]
public class when_a_user_denies_a_device(IdentityHost host) : IAsyncLifetime
{
    (HttpStatusCode Status, JsonElement Body) _result;

    public async ValueTask InitializeAsync()
    {
        var device = host.CreateBrowser();
        var started = await device.StartDevice();
        started.Status.ShouldEqual(HttpStatusCode.OK);
        var browser = host.CreateBrowser();
        var review = await browser.ReviewDevice(started.Body.GetProperty("user_code").GetString());
        using var denied = await browser.VerifyDevice(review, "deny");
        _result = await device.PollDevice(started.Body.GetProperty("device_code").GetString());
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_report_the_explicit_denial() => _result.Body.GetProperty("error").GetString().ShouldEqual("access_denied");
    [Fact] void should_not_issue_any_access_token() => _result.Body.TryGetProperty("access_token", out _).ShouldBeFalse();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_DeviceGrant;

[Requirement(Requirements.DeviceSingleUse)]
[Collection(IdentityCollection.Name)]
public class when_a_redeemed_device_code_is_reused(IdentityHost host) : IAsyncLifetime
{
    JsonElement _before;
    JsonElement _after;
    (HttpStatusCode Status, JsonElement Body) _replay;

    public async ValueTask InitializeAsync()
    {
        var device = host.CreateBrowser();
        var started = await device.StartDevice();
        started.Status.ShouldEqual(HttpStatusCode.OK);
        var browser = host.CreateBrowser();
        var review = await browser.ReviewDevice(started.Body.GetProperty("user_code").GetString());
        using var approved = await browser.VerifyDevice(review, "approve");
        approved.EnsureSuccessStatusCode();
        var code = started.Body.GetProperty("device_code").GetString();
        var tokens = await device.PollDevice(code);
        tokens.Status.ShouldEqual(HttpStatusCode.OK);
        var access = tokens.Body.GetProperty("access_token").GetString();
        (_, _before) = await device.Introspect(access);

        // Respect the real RFC 8628 polling interval so this reaches reuse detection, not slow_down.
        await Task.Delay(TimeSpan.FromSeconds(started.Body.GetProperty("interval").GetInt32() + 1));
        _replay = await host.CreateBrowser().PollDevice(code);
        (_, _after) = await device.Introspect(access);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_first_issue_a_live_access_token() => _before.GetProperty("active").GetBoolean().ShouldBeTrue();
    [Fact] void should_reject_the_replayed_code() => _replay.Body.GetProperty("error").GetString().ShouldEqual("invalid_grant");
    [Fact] void should_not_issue_another_access_token() => _replay.Body.TryGetProperty("access_token", out _).ShouldBeFalse();
    [Fact] void should_revoke_the_original_grant() => _after.GetProperty("active").GetBoolean().ShouldBeFalse();
}

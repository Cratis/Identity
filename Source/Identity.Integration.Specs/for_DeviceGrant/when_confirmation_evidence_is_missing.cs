// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_DeviceGrant;

[Collection(IdentityCollection.Name)]
public class when_confirmation_evidence_is_missing(IdentityHost host) : IAsyncLifetime
{
    HttpStatusCode _approval;
    JsonElement _poll;

    public async ValueTask InitializeAsync()
    {
        var device = host.CreateBrowser();
        var started = await device.StartDevice();
        started.Status.ShouldEqual(HttpStatusCode.OK);
        var browser = host.CreateBrowser();
        var review = await browser.ReviewDevice(started.Body.GetProperty("user_code").GetString());
        using var response = await browser.VerifyDevice(review, "approve", includeProof: false);
        _approval = response.StatusCode;
        (_, _poll) = await device.PollDevice(started.Body.GetProperty("device_code").GetString());
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_reject_confirmation_without_a_displayed_request_binding() => _approval.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_leave_the_device_unapproved() => _poll.GetProperty("error").GetString().ShouldEqual("authorization_pending");
}

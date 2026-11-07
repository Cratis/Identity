// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Requirements = Cratis.Identity.Integration.Conformance.Requirements;

namespace Cratis.Identity.Integration.for_DeviceGrant;

[Requirement(Requirements.DeviceBinding)]
[Collection(IdentityCollection.Name)]
public class when_another_client_redeems_a_device_code(IdentityHost host) : IAsyncLifetime
{
    (HttpStatusCode Status, JsonElement Body) _wrongClient;
    (HttpStatusCode Status, JsonElement Body) _rightClient;

    public async ValueTask InitializeAsync()
    {
        var device = host.CreateBrowser();
        var started = await device.StartDevice();
        started.Status.ShouldEqual(HttpStatusCode.OK);
        await using var scope = host.Services.CreateAsyncScope();
        var applications = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        const string otherClient = "device-binding-spec";
        await applications.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = otherClient,
            ClientType = ClientTypes.Public,
            Permissions = { Permissions.Endpoints.Token, Permissions.GrantTypes.DeviceCode }
        });
        var browser = host.CreateBrowser();
        var review = await browser.ReviewDevice(started.Body.GetProperty("user_code").GetString());
        using var approved = await browser.VerifyDevice(review, "approve");
        approved.EnsureSuccessStatusCode();
        var code = started.Body.GetProperty("device_code").GetString();
        _wrongClient = await device.PollDevice(code, otherClient);
        _rightClient = await device.PollDevice(code);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_reject_a_registered_client_that_did_not_start_the_request() => _wrongClient.Body.GetProperty("error").GetString().ShouldEqual("invalid_grant");
    [Fact] void should_not_issue_a_token_to_the_other_client() => _wrongClient.Body.TryGetProperty("access_token", out _).ShouldBeFalse();
    [Fact] void should_leave_the_code_usable_by_the_original_client() => _rightClient.Status.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_issue_the_original_client_a_token() => string.IsNullOrEmpty(_rightClient.Body.GetProperty("access_token").GetString()).ShouldBeFalse();
}

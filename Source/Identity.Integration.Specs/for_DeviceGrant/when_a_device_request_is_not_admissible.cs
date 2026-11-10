// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_DeviceGrant;

[Collection(IdentityCollection.Name)]
public class when_a_device_request_is_not_admissible(IdentityHost host) : IAsyncLifetime
{
    (HttpStatusCode Status, JsonElement Body) _unknownClient;
    (HttpStatusCode Status, JsonElement Body) _unknownResource;
    (HttpStatusCode Status, JsonElement Body) _unknownScope;
    (HttpStatusCode Status, JsonElement Body) _emptyScope;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateBrowser();
        _unknownClient = await browser.PostForm("/connect/device", [new("client_id", "unregistered-device-spec"), new("scope", "direct:read"), new("resource", IdentityHost.DirectMcp)]);
        _unknownResource = await browser.StartDevice(resource: "https://unknown.example/mcp");
        _unknownScope = await browser.StartDevice(scope: "unregistered:scope");
        _emptyScope = await browser.StartDevice(scope: string.Empty);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_reject_an_unknown_client() => _unknownClient.Body.GetProperty("error").GetString().ShouldEqual("invalid_client");
    [Fact] void should_reject_an_unknown_resource() => _unknownResource.Body.GetProperty("error").GetString().ShouldEqual("invalid_target");
    [Fact] void should_reject_an_unknown_scope() => _unknownScope.Body.GetProperty("error").GetString().ShouldEqual("invalid_scope");
    [Fact] void should_require_a_resource_scope() => _emptyScope.Body.GetProperty("error").GetString().ShouldEqual("invalid_scope");
    [Fact] void should_never_issue_device_codes_for_rejected_requests() => new[] { _unknownClient.Body, _unknownResource.Body, _unknownScope.Body, _emptyScope.Body }.Any(body => body.TryGetProperty("device_code", out _)).ShouldBeFalse();
}

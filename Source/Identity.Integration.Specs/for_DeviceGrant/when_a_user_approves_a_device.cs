// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_DeviceGrant;

[Requirement(Requirements.DeviceLifecycle)]
[Collection(IdentityCollection.Name)]
public class when_a_user_approves_a_device(IdentityHost host) : IAsyncLifetime
{
    JsonElement _start;
    string _review = string.Empty;
    (HttpStatusCode Status, JsonElement Body) _tokens;
    JsonElement _introspection;

    public async ValueTask InitializeAsync()
    {
        var device = host.CreateBrowser();
        var started = await device.StartDevice();
        started.Status.ShouldEqual(HttpStatusCode.OK);
        _start = started.Body;
        var browser = host.CreateBrowser();
        _review = await browser.ReviewDevice(_start.GetProperty("user_code").GetString());
        using var approved = await browser.VerifyDevice(_review, "approve");
        approved.EnsureSuccessStatusCode();
        _tokens = await device.PollDevice(_start.GetProperty("device_code").GetString());
        _tokens.Status.ShouldEqual(HttpStatusCode.OK);
        (_, _introspection) = await device.Introspect(_tokens.Body.GetProperty("access_token").GetString());
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_not_emit_a_code_in_a_url() => _start.TryGetProperty("verification_uri_complete", out _).ShouldBeFalse();
    [Fact] void should_return_the_manual_verification_uri() => _start.GetProperty("verification_uri").GetString().EndsWith("/device", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_advertise_a_five_second_poll_interval() => _start.GetProperty("interval").GetInt32().ShouldEqual(5);
    [Fact] void should_bound_the_code_lifetime() => (_start.GetProperty("expires_in").GetInt32() is > 0 and <= 300).ShouldBeTrue();
    [Fact] void should_display_the_client_and_scope() => (_review.Contains("Client:", StringComparison.Ordinal) && _review.Contains("direct:read", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_warn_about_unsolicited_codes() => _review.Contains("Only enter a code shown on a device you are signing in on. Never enter a code someone else sent you.", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_show_the_asserted_client_identifier() => _review.Contains("cratis-cli", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_emphasize_the_configured_issuer_host() => _review.Contains($"<strong>{new Uri(IdentityHost.Issuer).Host}</strong>", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_signed_in_upstream() => _review.Contains("(GitHub)", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_issue_a_live_access_token() => _introspection.GetProperty("active").GetBoolean().ShouldBeTrue();
    [Fact] void should_issue_a_refresh_token_for_offline_access() => string.IsNullOrEmpty(_tokens.Body.GetProperty("refresh_token").GetString()).ShouldBeFalse();
}

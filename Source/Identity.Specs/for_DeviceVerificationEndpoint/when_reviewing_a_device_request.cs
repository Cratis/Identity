// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using OpenIddict.Abstractions;

namespace Cratis.Identity.for_DeviceVerificationEndpoint;

public class when_reviewing_a_device_request : given.a_verification_request
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task should_show_the_entry_form_for_a_get_without_a_code(string? code)
    {
        _method = HttpMethods.Get;
        _code = code;
        var result = (ContentHttpResult)await Submit();
        result.ResponseContent.ShouldContain("Enter the code shown on the device you are connecting.");
        result.ResponseContent.ShouldContain("action=\"/id/device\"");
        _antiforgery.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == "IsRequestValidAsync").ShouldEqual(0);
    }

    [Theory]
    [InlineData("GET", "approve")]
    [InlineData("POST", "review")]
    public async Task should_display_review_without_a_posted_decision(string method, string? action)
    {
        _method = method;
        _action = action;
        var result = (ContentHttpResult)await Submit();
        if (method == "POST")
        {
            result.ResponseContent.ShouldContain("Approve device access");
            result.ResponseContent.ShouldContain("Device client");
            result.ResponseContent.ShouldContain("direct:read");
        }
        else
        {
            result.ResponseContent.ShouldContain("Enter the code");
        }
        result.ResponseContent.ShouldContain("action=\"/id/device\"");
    }

    [Theory]
    [InlineData("approve", true)]
    [InlineData("deny", true)]
    [InlineData("approve", false)]
    [InlineData("unexpected", true)]
    [InlineData("", true)]
    [InlineData(null, true)]
    [InlineData("deny", false)]
    public async Task should_require_a_recognized_action_and_bound_proof(string? action, bool validProof)
    {
        await CaptureProof();
        _action = action;
        if (!validProof) _proof = "tampered";
        var result = await Submit();
        if (action == "approve" && validProof)
        {
            var signIn = (SignInHttpResult)result;
            signIn.Principal.GetClaim(OpenIddictConstants.Claims.Subject).ShouldEqual(_account.Id.Value);
            signIn.Principal.GetClaim("tid").ShouldEqual("tenant");
            return;
        }

        if (action == "deny" && validProof)
        {
            var denial = (ForbidHttpResult)result;
            denial.Properties!.Items[OpenIddict.Server.AspNetCore.OpenIddictServerAspNetCoreConstants.Properties.Error].ShouldEqual("access_denied");
            return;
        }

        ((ContentHttpResult)result).StatusCode.ShouldEqual(400);
    }

    [Theory]
    [InlineData("unknown-tenant")]
    [InlineData("missing-device")]
    [InlineData("missing-expiry")]
    [InlineData("expired-device")]
    public async Task should_refuse_an_unusable_approval(string branch)
    {
        await CaptureProof();
        _action = "approve";
        var failures = new Dictionary<string, Action>
        {
            ["unknown-tenant"] = () => _tenant = "other",
            ["missing-device"] = () => _tokens.FindByIdAsync("device-token", Arg.Any<CancellationToken>()).Returns((object?)null),
            ["missing-expiry"] = () => _tokens.GetExpirationDateAsync(Arg.Any<object>(), Arg.Any<CancellationToken>()).Returns((DateTimeOffset?)null),
            ["expired-device"] = () => _tokens.GetExpirationDateAsync(Arg.Any<object>(), Arg.Any<CancellationToken>()).Returns(DateTimeOffset.UtcNow.AddMinutes(-1))
        };
        failures[branch]();
        ((ContentHttpResult)await Submit()).StatusCode.ShouldEqual(400);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("")]
    public async Task should_complete_a_get_with_any_done_parameter(string done)
    {
        _method = HttpMethods.Get;
        _done = done;
        ((ContentHttpResult)await Submit()).ResponseContent.ShouldContain("Verification complete");
    }
}

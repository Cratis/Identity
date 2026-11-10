// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Tenants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Cratis.Identity.for_DeviceVerificationEndpoint;

public class when_a_verification_cannot_continue : given.a_verification_request
{
    [Theory]
    [InlineData("antiforgery")]
    [InlineData("failed-authentication")]
    [InlineData("missing-principal")]
    [InlineData("closed-admission")]
    [InlineData("missing-client")]
    [InlineData("missing-resource")]
    [InlineData("denied-policy")]
    [InlineData("changed-scopes")]
    [InlineData("missing-application")]
    [InlineData("tenant-unavailable")]
    [InlineData("no-tenants")]
    [InlineData("missing-cookie")]
    [InlineData("empty-cookie")]
    public async Task should_return_the_same_safe_failure(string branch)
    {
        Failures()[branch]();
        var result = (ContentHttpResult)await Submit();
        result.StatusCode.ShouldEqual(400);
        result.ResponseContent.ShouldContain("That code is invalid or has expired.");
        _applications.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == "GetDisplayNameAsync").ShouldEqual(0);
    }

    [Fact]
    public async Task should_challenge_a_missing_site_session()
    {
        _accounts.FindActive(Arg.Any<AccountSubject>(), Arg.Any<CancellationToken>()).Returns((Accounts.Account?)null);
        var result = (ChallengeHttpResult)await Submit();
        result.AuthenticationSchemes.ShouldContain("GitHub");
        result.Properties!.RedirectUri.ShouldEqual("/id/device");
    }

    [Theory]
    [InlineData(false)]
    public async Task should_hide_a_disabled_endpoint(bool enabled)
    {
        _options.DeviceGrant.Enabled = enabled;
        (await Submit()).ShouldBeOfExactType<NotFound>();
        _authentication.ReceivedCalls().ShouldBeEmpty();
    }

    Dictionary<string, Action> Failures() => new()
    {
        ["antiforgery"] = () => _antiforgery.IsRequestValidAsync(Arg.Any<HttpContext>()).Returns(false),
        ["failed-authentication"] = () => _authentication.AuthenticateAsync(Arg.Any<HttpContext>(), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme).Returns(AuthenticateResult.Fail("invalid")),
        ["missing-principal"] = () => _authentication.AuthenticateAsync(Arg.Any<HttpContext>(), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme).Returns(AuthenticateResult.NoResult()),
        ["closed-admission"] = () => _log.RecoveryEpoch(Arg.Any<CancellationToken>()).Returns(8L),
        ["missing-client"] = () => _principal.SetClaim(OpenIddictConstants.Claims.ClientId, (string?)null),
        ["missing-resource"] = () => _principal.SetClaim(Device.DeviceAuthorizationEndpoint.ResourceClaim, (string?)null),
        ["denied-policy"] = () => _policy.Evaluate(Arg.Any<GrantRequest>()).Returns(new NotGrantable("invalid_scope", "denied")),
        ["changed-scopes"] = () => _principal.SetScopes(["direct:read", "unexpected"]),
        ["missing-application"] = () => _applications.FindByClientIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((object?)null),
        ["tenant-unavailable"] = () => _tenants.SelectableTenants(Arg.Any<Accounts.Account>(), Arg.Any<Resources.ResourceServer>(), Arg.Any<CancellationToken>()).Returns(new TenantsUnavailable(TenantsUnavailableReason.Unreachable)),
        ["no-tenants"] = () => _tenants.SelectableTenants(Arg.Any<Accounts.Account>(), Arg.Any<Resources.ResourceServer>(), Arg.Any<CancellationToken>()).Returns(new TenantsListed([])),
        ["missing-cookie"] = () => _cookie = null,
        ["empty-cookie"] = () => _cookie = string.Empty
    };
}

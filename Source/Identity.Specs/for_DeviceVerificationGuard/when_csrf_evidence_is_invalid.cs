// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Device;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_DeviceVerificationGuard;

public class when_csrf_evidence_is_invalid : for_DeviceGrantAdmission.given.an_approved_device
{
    ProcessAuthenticationContext _context;
    IAntiforgery _antiforgery;
    HttpContext _http;

    async Task Because()
    {
        var authentication = Substitute.For<IAuthenticationService>();
        authentication.AuthenticateAsync(default!, default).ReturnsForAnyArgs(AuthenticateResult.NoResult());
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IAuthenticationService)).Returns(authentication);
        _http = new DefaultHttpContext { RequestServices = services };
        _http.Request.Method = HttpMethods.Post;
        _antiforgery = Substitute.For<IAntiforgery>();
        _antiforgery.IsRequestValidAsync(_http).Returns(false);
        var transaction = new OpenIddictServerTransaction
        {
            EndpointType = OpenIddictServerEndpointType.EndUserVerification,
            Logger = NullLogger.Instance,
            Options = new OpenIddictServerOptions(),
            Request = new OpenIddictRequest { UserCode = "BCDFGHJK" }
        };
        _context = new ProcessAuthenticationContext(transaction) { UserCode = "BCDFGHJK" };
        _tokens.ClearReceivedCalls();
        var guard = new DeviceVerificationGuard(new HttpContextAccessor { HttpContext = _http }, _antiforgery, null!, null!, _admission);
        await guard.HandleAsync(_context);
    }

    [Fact] void should_test_with_complete_recovery_admission() => _baseline.ShouldBeTrue();
    [Fact] void should_reject_the_forgery() => _context.Error.ShouldEqual("invalid_request");
    [Fact] void should_check_csrf() => _antiforgery.Received(1).IsRequestValidAsync(_http);
    [Fact] void should_not_look_up_any_code() => _tokens.DidNotReceiveWithAnyArgs().FindByIdAsync(default!, TestContext.Current.CancellationToken);
    [Fact] void should_run_before_openiddict_can_look_up_the_code() => (DeviceVerificationGuard.Descriptor.Order < OpenIddictServerHandlers.ValidateUserCode.Descriptor.Order).ShouldBeTrue();
}

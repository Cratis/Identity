// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Device;
using Cratis.Identity.RevocationSpecs;
using Microsoft.Extensions.Logging.Abstractions;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_RedeemDeviceCodeOnce.given;

public class a_device_redemption : for_DeviceGrantAdmission.given.an_approved_device
{
    private protected RedeemDeviceCodeOnce _handler;
    private protected SharedFenceStore _authority;
    protected for_DeviceGrantRateLimiter.given.controlled_clock _clock;
    protected ProcessSignInContext _context;

    void Establish()
    {
        _clock = new();
        _authority = new();
        _fences.Read("authorization", Arg.Any<CancellationToken>()).Returns(_ => _authority.Read("authorization", CancellationToken.None));
        var grants = new GrantRevocation(_tokens, _authorizations, _authority, FenceDoubles.Confirmation(_authority));
        _handler = new(_tokens, grants, _admission, _clock);
        _tokens.GetExpirationDateAsync(_token, Arg.Any<CancellationToken>()).Returns(_clock.GetUtcNow().AddMinutes(10));
        _authorizations.TryRevokeAsync(_authorization, Arg.Any<CancellationToken>()).Returns(true);
        _context = Request();
    }

    protected ProcessSignInContext Request()
    {
        var transaction = new OpenIddictServerTransaction
        {
            EndpointType = OpenIddictServerEndpointType.Token,
            Logger = NullLogger.Instance,
            Options = new OpenIddictServerOptions(),
            Request = new OpenIddictRequest { GrantType = GrantTypes.DeviceCode }
        };
        transaction.SetProperty(typeof(ProcessAuthenticationContext).FullName, new ProcessAuthenticationContext(transaction) { DeviceCodePrincipal = _principal.Clone() });
        return new ProcessSignInContext(transaction) { Principal = _principal.Clone() };
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Device;
using Microsoft.Extensions.Logging.Abstractions;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_DeviceGrantRateLimiter.given;

public class a_polling_request : a_shared_limiter
{
    private protected DevicePollingGuard _guard;
    protected ProcessAuthenticationContext _context;

    void Establish()
    {
        _guard = new(_first);
        _context = new(new OpenIddictServerTransaction
        {
            EndpointType = OpenIddictServerEndpointType.Token,
            Logger = NullLogger.Instance,
            Options = new OpenIddictServerOptions(),
            Request = new OpenIddictRequest { GrantType = GrantTypes.DeviceCode, ClientId = "device-client" }
        })
        {
            DeviceCode = "opaque-device-test-value",
            DeviceCodePrincipal = new ClaimsPrincipal(new ClaimsIdentity("test"))
                .SetTokenId("validated-device-entry").SetPresenters("device-client")
        };
    }
}

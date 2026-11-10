// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_DeviceGrantRateLimiter.given;

public class a_polling_admission_request : for_DeviceGrantAdmission.given.an_approved_device
{
    protected ProcessAuthenticationContext _context;

    void Establish() => _context = new(new OpenIddictServerTransaction
    {
        EndpointType = OpenIddictServerEndpointType.Token,
        Logger = NullLogger.Instance,
        Options = new OpenIddictServerOptions(),
        Request = new OpenIddictRequest { GrantType = GrantTypes.DeviceCode, ClientId = "device-client" }
    });
}

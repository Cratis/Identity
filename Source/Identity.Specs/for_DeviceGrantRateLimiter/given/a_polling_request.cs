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
    protected ValidateTokenContext _context;
    protected IOpenIddictTokenManager _tokens;
    protected object _token;

    void Establish()
    {
        _tokens = Substitute.For<IOpenIddictTokenManager>();
        _token = new object();
        _tokens.FindByIdAsync("validated-device-entry", Arg.Any<CancellationToken>()).Returns(_token);
        _tokens.HasStatusAsync(_token, Statuses.Inactive, Arg.Any<CancellationToken>()).Returns(true);
        _guard = new(_first, _tokens);
        _context = new(new OpenIddictServerTransaction
        {
            EndpointType = OpenIddictServerEndpointType.Token,
            Logger = NullLogger.Instance,
            Options = new OpenIddictServerOptions { TimeProvider = _clock },
            Request = new OpenIddictRequest { GrantType = GrantTypes.DeviceCode, ClientId = "device-client" }
        })
        {
            Token = "opaque-device-test-value",
            TokenValidationParameters = new(),
            TokenId = "validated-device-entry",
            Principal = new ClaimsPrincipal(new ClaimsIdentity("test"))
                .SetTokenId("validated-device-entry").SetPresenters("device-client")
                .SetTokenType(TokenTypeIdentifiers.Private.DeviceCode),
            ValidTokenTypes = { TokenTypeIdentifiers.Private.DeviceCode }
        };
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.given;
using Microsoft.Extensions.Logging.Abstractions;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_RejectExpiredTokenEntries.given;

public class a_token_to_validate : Specification
{
    protected const string TokenId = "token-1";

    protected IOpenIddictTokenManager _tokens;
    protected RejectExpiredTokenEntries _handler;
    protected FixedTimeProvider _time;
    protected object _entry;

    void Establish()
    {
        _tokens = Substitute.For<IOpenIddictTokenManager>();
        _time = new FixedTimeProvider(KeyMaterial.Now);
        _handler = new RejectExpiredTokenEntries(_tokens, _time);
        _entry = new object();
        _tokens.FindByIdAsync(TokenId, Arg.Any<CancellationToken>()).Returns(_entry);
    }

    protected void Expires(DateTimeOffset? expiration) => _tokens.GetExpirationDateAsync(_entry, Arg.Any<CancellationToken>()).Returns(expiration);

    protected static ValidateTokenContext Context(OpenIddictServerEndpointType endpoint = OpenIddictServerEndpointType.Token)
    {
        var transaction = new OpenIddictServerTransaction
        {
            EndpointType = endpoint,
            Logger = NullLogger.Instance,
            Options = new OpenIddictServerOptions(),
            Request = new OpenIddictRequest()
        };
        return new ValidateTokenContext(transaction) { Token = "token", TokenId = TokenId };
    }
}

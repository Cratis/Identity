// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_RedeemDeviceCodeOnce;

public class when_the_code_has_expired_before_ttl_cleanup : given.a_device_redemption
{
    void Establish() => _tokens.GetExpirationDateAsync(_token, Arg.Any<CancellationToken>()).Returns(_clock.GetUtcNow());
    async Task Because() => await _handler.HandleAsync(_context);

    [Fact] void should_reject_the_expired_code() => _context.Error.ShouldEqual("expired_token");
    [Fact] void should_not_attempt_a_cas_on_an_expired_code() => _tokens.DidNotReceive().TryRedeemAsync(_token, Arg.Any<CancellationToken>());
}

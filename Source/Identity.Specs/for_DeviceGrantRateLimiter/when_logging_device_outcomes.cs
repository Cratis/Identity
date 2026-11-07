// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_logging_device_outcomes : given.a_shared_limiter
{
    async Task Because()
    {
        (await _first.Poll("opaque-device-test-value", CancellationToken.None)).ShouldEqual(false);
        (await _second.Poll("opaque-device-test-value", CancellationToken.None)).ShouldEqual(true);
        (await _first.Verify("BCDFGHJK", "account-test-value", null, CancellationToken.None)).ShouldBeFalse();
        _store.Unavailable = true;
        (await _first.Poll("opaque-device-test-value", CancellationToken.None)).ShouldBeNull();
    }

    [Fact] void should_capture_each_exercised_outcome() => _logger.Messages.Count.ShouldEqual(3);
    [Fact] void should_not_log_any_structured_request_values() => _logger.Values.ShouldBeEmpty();
    [Fact] void should_not_log_codes_accounts_or_credentials() => new[] { "opaque-device-test-value", "BCDFGHJK", "account-test-value" }.Any(value => string.Join('\n', _logger.Messages).Contains(value, StringComparison.Ordinal)).ShouldBeFalse();
}

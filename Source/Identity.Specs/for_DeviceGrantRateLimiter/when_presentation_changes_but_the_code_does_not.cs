// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DeviceGrantRateLimiter;

public class when_presentation_changes_but_the_code_does_not : given.a_shared_limiter
{
    void Establish() => _options.DeviceGrant.AttemptsPerCode = 1;
    async Task Because() => _results = [await Verify(), await _second.Verify("bcdf-ghjk", "another-account", "192.0.2.2", CancellationToken.None)];

    [Fact] void should_first_admit_the_code() => _results[0].ShouldBeTrue();
    [Fact] void should_share_the_same_code_budget_after_normalization() => _results[1].ShouldBeFalse();
}

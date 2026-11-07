// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Operational.for_LegacyAccountStoreGuard;

public class when_the_probe_times_out : given.a_legacy_store_guard
{
    void Establish() => _probe.Throws(ConfiguredConnection, new LegacyStoreProbeFailed(LegacyStoreFailure.Timeout, null));

    Task Because() => Start();

    [Fact] void should_refuse_to_start() => _error.ShouldBeOfExactType<LegacyAccountStoreUnverifiable>();
    [Fact] void should_state_the_timeout() => _error.Message.ShouldContain("timeout");
}

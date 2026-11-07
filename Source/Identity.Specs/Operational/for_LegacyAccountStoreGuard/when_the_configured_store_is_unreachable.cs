// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Operational.for_LegacyAccountStoreGuard;

public class when_the_configured_store_is_unreachable : given.a_legacy_store_guard
{
    void Establish() => _probe.Throws(ConfiguredConnection, new LegacyStoreProbeFailed(LegacyStoreFailure.Unreachable, new InvalidOperationException("canary-secret")));

    Task Because() => Start();

    [Fact] void should_refuse_to_start() => _error.ShouldBeOfExactType<LegacyAccountStoreUnverifiable>();
    [Fact] void should_state_the_reason() => _error.Message.ShouldContain("unreachable");
    [Fact] void should_say_it_could_not_be_checked() => _error.Message.ShouldContain("could not be checked");
    [Fact] void should_not_leak_the_driver_message() => (_error.Message + LoggedText).ShouldNotContain("canary-secret");
}

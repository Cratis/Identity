// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Operational.for_LegacyAccountStoreGuard;

public class when_the_probe_fails_unexpectedly : given.a_legacy_store_guard
{
    void Establish() => _probe.Throws(ConfiguredConnection, new InvalidOperationException("mongodb://identity-user:canary-secret@db.internal"));

    Task Because() => Start();

    [Fact] void should_refuse_to_start() => _error.ShouldBeOfExactType<LegacyAccountStoreUnverifiable>();
    [Fact] void should_not_leak_the_exception_message() => (_error.Message + LoggedText).ShouldNotContain("canary-secret");
}

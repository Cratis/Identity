// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Operational.for_LegacyAccountStoreGuard;

public class when_nothing_is_configured : given.a_legacy_store_guard
{
    void Establish() => NothingConfigured();

    Task Because() => Start();

    [Fact] void should_start() => _error.ShouldBeNull();
    [Fact] void should_check_the_default_location_once() => _probe.Calls.ShouldContainOnly(("mongodb://localhost:27017", "cratis-identity"));
}

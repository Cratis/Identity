// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Operational.for_LegacyAccountStoreGuard;

public class when_the_collection_is_absent : given.a_legacy_store_guard
{
    Task Because() => Start();

    [Fact] void should_start() => _error.ShouldBeNull();
    [Fact] void should_check_both_locations() => _probe.Calls.Count.ShouldEqual(2);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Operational.for_LegacyAccountStoreGuard;

public class when_the_store_is_empty : given.a_legacy_store_guard
{
    void Establish()
    {
        _probe.Returns(ConfiguredConnection, LegacyStoreState.Empty);
        _probe.Returns(DefaultConnection, LegacyStoreState.Empty);
    }

    Task Because() => Start();

    [Fact] void should_start() => _error.ShouldBeNull();
    [Fact] void should_check_the_configured_and_the_default_location() => _probe.Calls.Select(call => call.ConnectionString).ShouldContainOnly(ConfiguredConnection, DefaultConnection);
}

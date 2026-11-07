// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Operational.for_LegacyAccountStoreGuard;

public class when_the_default_store_is_unreachable_but_the_configured_is_empty : given.a_legacy_store_guard
{
    void Establish()
    {
        _probe.Returns(ConfiguredConnection, LegacyStoreState.Empty);
        _probe.Throws(DefaultConnection, new LegacyStoreProbeFailed(LegacyStoreFailure.Unreachable, null));
    }

    Task Because() => Start();

    [Fact] void should_refuse_to_start() => _error.ShouldBeOfExactType<LegacyAccountStoreUnverifiable>();
    [Fact] void should_name_the_default_location() => _error.Message.ShouldContain("default");
}

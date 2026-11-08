// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Operational.for_LegacyAccountStoreGuard;

public class when_the_default_store_has_accounts_and_the_configured_is_empty : given.a_legacy_store_guard
{
    void Establish()
    {
        _probe.Returns(ConfiguredConnection, LegacyStoreState.Empty);
        _probe.Returns(DefaultConnection, LegacyStoreState.HasAccounts);
    }

    Task Because() => Start();

    [Fact] void should_refuse_to_start() => _error.ShouldBeOfExactType<LegacyAccountStoreFound>();
    [Fact] void should_name_the_default_location() => _error.Message.ShouldContain("default");
}

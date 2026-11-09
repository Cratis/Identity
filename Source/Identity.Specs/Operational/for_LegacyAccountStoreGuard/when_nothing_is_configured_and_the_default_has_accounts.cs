// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Operational.for_LegacyAccountStoreGuard;

public class when_nothing_is_configured_and_the_default_has_accounts : given.a_legacy_store_guard
{
    void Establish()
    {
        NothingConfigured();
        _probe.Returns(DefaultConnection, LegacyStoreState.HasAccounts);
    }

    Task Because() => Start();

    [Fact] void should_refuse_to_start() => _error.ShouldBeOfExactType<LegacyAccountStoreFound>();
}

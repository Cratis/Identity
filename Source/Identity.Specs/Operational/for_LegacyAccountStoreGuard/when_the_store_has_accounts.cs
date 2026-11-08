// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Identity.Operational.for_LegacyAccountStoreGuard;

public class when_the_store_has_accounts : given.a_legacy_store_guard
{
    void Establish() => _probe.Returns(ConfiguredConnection, LegacyStoreState.HasAccounts);

    Task Because() => Start();

    [Fact] void should_refuse_to_start() => _error.ShouldBeOfExactType<LegacyAccountStoreFound>();
    [Fact] void should_name_the_configured_location() => _error.Message.ShouldContain("configured");
    [Fact] void should_name_the_opt_out() => _error.Message.ShouldContain("Identity:Accounts:ConfirmNoLegacyMongoStore");
    [Fact] void should_not_leak_the_credentials() => (_error.Message + LoggedText).ShouldNotContain("canary-secret");
    [Fact] void should_not_leak_the_host() => (_error.Message + LoggedText).ShouldNotContain("db.internal");
    [Fact] void should_not_leak_the_database_name() => (_error.Message + LoggedText).ShouldNotContain("configured-db");
    [Fact] void should_log_critical() => _logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Critical);
}

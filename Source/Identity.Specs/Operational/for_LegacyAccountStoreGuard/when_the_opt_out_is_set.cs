// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Identity.Operational.for_LegacyAccountStoreGuard;

public class when_the_opt_out_is_set : given.a_legacy_store_guard
{
    void Establish()
    {
        _options.Accounts.ConfirmNoLegacyMongoStore = true;
        _probe.Returns(ConfiguredConnection, LegacyStoreState.HasAccounts);
    }

    Task Because() => Start();

    [Fact] void should_start() => _error.ShouldBeNull();
    [Fact] void should_not_probe_anything() => _probe.Calls.ShouldBeEmpty();
    [Fact] void should_log_one_warning() => _logger.Entries.Count(entry => entry.Level == LogLevel.Warning).ShouldEqual(1);
}

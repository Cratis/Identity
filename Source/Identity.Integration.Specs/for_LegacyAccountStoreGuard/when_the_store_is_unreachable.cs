// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Identity.Operational;

namespace Cratis.Identity.Integration.for_LegacyAccountStoreGuard;

[Collection(IdentityCollection.Name)]
public class when_the_store_is_unreachable(IdentityHost host)
{
    [Fact]
    public async Task should_refuse_to_start_within_the_probe_bounds()
    {
        var scenario = new given.LegacyGuardScenario(host);
        var clock = Stopwatch.StartNew();

        var error = await scenario.Start("mongodb://127.0.0.1:1");

        error.ShouldBeOfExactType<LegacyAccountStoreUnverifiable>();
        (clock.Elapsed < TimeSpan.FromSeconds(30)).ShouldBeTrue();
    }
}

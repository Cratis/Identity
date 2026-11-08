// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Identity.for_HostedSharedConfiguration;

public class when_checking_recovery_readiness : Specification
{
    [Fact]
    async Task should_stay_unhealthy_until_recovery_is_proven()
    {
        var gate = new RecoveryGate();
        var readiness = new RecoveryReadiness(gate);
        (await readiness.CheckHealthAsync(new(), TestContext.Current.CancellationToken)).Status.ShouldEqual(HealthStatus.Unhealthy);
        gate.Open(false);
        (await readiness.CheckHealthAsync(new(), TestContext.Current.CancellationToken)).Status.ShouldEqual(HealthStatus.Unhealthy);
        gate.Open(true);
        (await readiness.CheckHealthAsync(new(), TestContext.Current.CancellationToken)).Status.ShouldEqual(HealthStatus.Healthy);
        gate.Close();
        (await readiness.CheckHealthAsync(new(), TestContext.Current.CancellationToken)).Status.ShouldEqual(HealthStatus.Unhealthy);
    }
}

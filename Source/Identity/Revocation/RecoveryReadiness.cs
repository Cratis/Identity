// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Identity.Revocation;

/// <summary>Hosts must expose this health check on their readiness endpoint, not their liveness endpoint.</summary>
/// <param name="gate">The host continuity barrier.</param>
internal sealed class RecoveryReadiness(RecoveryGate gate) : IHealthCheck
{
    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(gate.Ready ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Identity restriction recovery is unproven."));
}

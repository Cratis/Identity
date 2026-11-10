// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation.Operational;

/// <summary>
/// Creates the independent recovery log without exposing MongoDB through domain registration signatures.
/// </summary>
internal static class IndependentFenceLogFactory
{
    /// <summary>
    /// Creates the log while keeping its database and startup proof deferred.
    /// </summary>
    /// <param name="services">The registered dependencies.</param>
    /// <returns>The independent recovery log.</returns>
    internal static MongoIndependentFenceLog Create(IServiceProvider services) => new(
        () => services.GetRequiredService<IndependentFenceLogConnection>().Database,
        services.GetRequiredService<TimeProvider>(),
        () => services.GetRequiredService<RecoverySession>().Verified);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Operational;

/// <summary>Looks, read-only, for the legacy MongoDB account collection.</summary>
internal interface ILegacyAccountStoreProbe
{
    /// <summary>Checks one MongoDB location without changing it or reading document contents.</summary>
    /// <param name="connectionString">The connection string of the location.</param>
    /// <param name="database">The database that held the legacy accounts collection.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>What was found.</returns>
    /// <exception cref="LegacyStoreProbeFailed">The location could not be checked.</exception>
    Task<LegacyStoreState> Check(string connectionString, string database, CancellationToken cancellationToken);
}

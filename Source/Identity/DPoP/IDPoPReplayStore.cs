// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.DPoP;

/// <summary>A shared, durable insert-if-absent proof reservation; failures must propagate.</summary>
internal interface IDPoPReplayStore
{
    /// <summary>Reserves a scoped proof ID until its full replay horizon has elapsed.</summary>
    /// <param name="domain">The validator domain.</param>
    /// <param name="origin">The configured origin.</param>
    /// <param name="jkt">The proof key.</param>
    /// <param name="jti">The proof identifier.</param>
    /// <param name="expiresAt">The acceptance time plus the replay horizon.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True for the sole winner, false for replay.</returns>
    Task<bool> TryReserve(string domain, string origin, string jkt, string jti, DateTimeOffset expiresAt, CancellationToken cancellationToken);
}

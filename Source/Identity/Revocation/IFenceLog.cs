// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>The replaceable durable log boundary; backup independence is handled separately, not by this log.</summary>
internal interface IFenceLog
{
    /// <summary>Appends once, or verifies identical prior content; conflicts must throw.</summary>
    /// <param name="fence">The complete immutable fence.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The original sequence.</returns>
    Task<long> Append(GrantFence fence, CancellationToken cancellationToken);
}

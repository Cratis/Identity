// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>Shared by request acknowledgement and crash recovery; never treats uncertainty as confirmation.</summary>
/// <param name="store">The operational authority.</param>
/// <param name="log">The replaceable durable log.</param>
internal sealed class ConfirmFenceLog(IFenceStore store, IFenceLog log)
{
    /// <summary>Confirms precisely the immutable restriction that the log accepted.</summary>
    /// <param name="fence">The restriction.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The durable confirmation.</returns>
    /// <exception cref="FenceLogConflict">The log acknowledged a different sequence.</exception>
    internal async Task Confirm(GrantFence fence, CancellationToken cancellationToken)
    {
        if (await log.Append(fence, cancellationToken) != fence.Sequence)
        {
            throw new FenceLogConflict();
        }

        await store.ConfirmLog(fence, cancellationToken);
    }
}

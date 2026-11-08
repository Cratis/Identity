// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>Bridges the acknowledgement protocol to complete immutable independent records.</summary>
/// <param name="store">The operational source of the original persisted payloads.</param>
/// <param name="log">The independent log.</param>
internal sealed class IndependentFenceLogAppender(IFenceStore store, IIndependentFenceLog log) : IFenceLog
{
    /// <inheritdoc/>
    public async Task<long> Append(GrantFence fence, CancellationToken cancellationToken)
    {
        var state = await store.Read(fence.GrantId, cancellationToken) ?? throw new RevocationPending();
        if (state.Fence != fence)
        {
            throw new FenceLogConflict();
        }

        return await log.Append(FenceRecordFactory.Record(state), cancellationToken);
    }
}

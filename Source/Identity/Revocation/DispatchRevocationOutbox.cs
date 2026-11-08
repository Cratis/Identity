// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>Replayable crash recovery: confirm log, keyed append, then delivery mark. Never authorizes requests.</summary>
/// <param name="store">The primary authority and outbox.</param>
/// <param name="confirm">The durable log confirmation.</param>
/// <param name="writer">The keyed source-fact writer.</param>
internal sealed class DispatchRevocationOutbox(IFenceStore store, ConfirmFenceLog confirm, IRevocationFactWriter writer)
{
    /// <summary>Recovers one bounded durable batch, isolating failures without reporting partial success.</summary>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The recovery pass.</returns>
    internal async Task Dispatch(CancellationToken cancellationToken)
    {
        Exception? failure = null;
        foreach (var state in await store.Undelivered(100, cancellationToken))
        {
            try
            {
                await Dispatch(state, cancellationToken);
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                failure = PreferConflict(failure, exception);
            }
        }

        if (failure is not null)
        {
            throw failure;
        }
    }

    /// <summary>Tests remaining work without treating a bounded batch as full replay.</summary>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Whether any delivery remains uncertain.</returns>
    internal async Task<bool> Pending(CancellationToken cancellationToken) => (await store.Undelivered(1, cancellationToken)).Count != 0;

    static Exception PreferConflict(Exception? previous, Exception current) => current is OutboxContentConflict or FenceLogConflict ? current : previous ?? current;

    async Task Dispatch(GrantState state, CancellationToken cancellationToken)
    {
        if (!GrantStateValidation.IsKnown(state))
        {
            throw new RevocationPending();
        }

        if (state.Fence is { } fence && state.Outbox.Any(entry => entry.State == OutboxState.Pending))
        {
            await confirm.Confirm(fence, cancellationToken);
        }

        var confirmed = await store.Read(state.GrantId, cancellationToken) ?? throw new RevocationPending();
        if (!GrantStateValidation.IsKnown(confirmed))
        {
            throw new RevocationPending();
        }

        Exception? failure = null;
        foreach (var entry in confirmed.Outbox.Where(entry => entry.State != OutboxState.Delivered))
        {
            try
            {
                if (entry.State != OutboxState.LogConfirmed)
                {
                    throw new RevocationPending();
                }

                await writer.Append(entry, cancellationToken);
                await store.Delivered(state.GrantId, entry.Id, cancellationToken);
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                failure = PreferConflict(failure, exception);
            }
        }

        if (failure is not null)
        {
            throw failure;
        }
    }
}

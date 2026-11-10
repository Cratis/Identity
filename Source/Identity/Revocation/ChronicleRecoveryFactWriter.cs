// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Identity.Revocation.Events;

namespace Cratis.Identity.Revocation;

/// <summary>Uses the same draft #18 append contract as restriction delivery.</summary>
/// <param name="eventStore">The configured Chronicle event store.</param>
internal sealed class ChronicleRecoveryFactWriter(IEventStore eventStore) : IRecoveryFactWriter
{
    /// <inheritdoc/>
    public Task Bind(string instanceId, CancellationToken cancellationToken) =>
        Append(new FenceLogCustodyEstablished(instanceId), "identity-fence-log-custody-v1", DateTimeOffset.UnixEpoch, cancellationToken);

    /// <inheritdoc/>
    public Task Publish(RecoveryEpochIntent intent, CancellationToken cancellationToken) =>
        Append(
            new RealmRecoveryEpochAdvanceRecorded(new((ulong)intent.Epoch), new("StoreRestored"), new(intent.FenceId), new((ulong)intent.FenceSequence), new(intent.FenceId)),
            $"{intent.FenceId:D}:{nameof(RealmRecoveryEpochAdvanceRecorded)}",
            intent.OccurredAt,
            cancellationToken);

    /// <inheritdoc/>
    public Task Checkpoint(string instanceId, long publishedThrough, CancellationToken cancellationToken) =>
        Append(new FencePublicationCheckpoint(instanceId, new((ulong)publishedThrough)), $"identity-fence-publication-v1:{publishedThrough}", DateTimeOffset.UnixEpoch, cancellationToken);

    async Task Append(object fact, string key, DateTimeOffset occurredAt, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sequence = eventStore.GetEventSequence(EventSequenceId.Log);
        if (!sequence.SupportsIdempotentAppends)
        {
            throw new RevocationPending();
        }

        var result = await sequence.AppendOnce(new EventSourceId("identity-recovery"), fact, new IdempotencyKey(key), occurred: occurredAt);
        if (result.HasIdempotencyConflicts)
        {
            throw new OutboxContentConflict();
        }

        if (!result.IsSuccess)
        {
            throw new RevocationPending();
        }
    }
}

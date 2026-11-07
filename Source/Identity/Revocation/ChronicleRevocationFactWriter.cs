// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Identity.Revocation;

/// <summary>Requires a kernel advertising keyed append support; there is deliberately no plain-append fallback.</summary>
/// <param name="eventStore">The source event store.</param>
internal sealed class ChronicleRevocationFactWriter(IEventStore eventStore) : IRevocationFactWriter
{
    /// <inheritdoc/>
    public async Task Append(OutboxEntry entry, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sequence = eventStore.GetEventSequence(EventSequenceId.Log);
        if (!sequence.SupportsIdempotentAppends)
        {
            throw new RevocationPending();
        }

        var result = await sequence.AppendOnce(new EventSourceId(entry.GrantId.ToString("D")), entry.Fact(), new IdempotencyKey(entry.Id), occurred: entry.OccurredAt);

        // In 19.111.0 IsSuccess does not include HasIdempotencyConflicts; check both explicitly.
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

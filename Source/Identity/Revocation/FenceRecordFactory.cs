// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>Persists the original restriction payloads; recovery must never mint replacement fact identities.</summary>
internal static class FenceRecordFactory
{
    /// <summary>Creates the initial restriction intents.</summary>
    /// <param name="fence">The immutable committed fence.</param>
    /// <returns>Retry-stable restriction intents.</returns>
    internal static OutboxEntry[] Entries(GrantFence fence)
    {
        var kinds = fence.Reason == RevocationReason.RefreshTokenReused
            ? new[] { FactKind.AuthorizationRevocation, FactKind.FamilyRevocation, FactKind.Reuse }
            : [FactKind.AuthorizationRevocation, FactKind.FamilyRevocation];
        return kinds.Select(kind => OutboxEntry.Create(kind, fence.GrantId, fence.Subject, fence.RecordedAt, fence: fence)).ToArray();
    }

    /// <summary>Captures original payloads from operational authority.</summary>
    /// <param name="state">The known fenced state.</param>
    /// <returns>The complete immutable independent record.</returns>
    /// <exception cref="FenceLogConflict">The state lacks a valid permanent denial.</exception>
    internal static FenceRecord Record(GrantState state)
    {
        var fence = state.Fence ?? throw new FenceLogConflict();
        var entries = state.Outbox.Where(entry => entry.Fence == fence).Select(entry => entry with { State = OutboxState.Pending }).ToArray();
        var record = new FenceRecord(fence, entries.Select(entry => entry.Id).ToArray(), entries);
        Validate(record);
        return record;
    }

    /// <summary>Checks the complete closed restriction schema.</summary>
    /// <param name="record">The independent record.</param>
    /// <exception cref="FenceLogConflict">The payload is incomplete or inconsistent.</exception>
    internal static void Validate(FenceRecord record)
    {
        if (record.Intents is not { Length: > 0 } || !record.IntentIds.SequenceEqual(record.Intents.Select(entry => entry.Id)) ||
            record.Intents.Any(entry => entry.State != OutboxState.Pending))
        {
            throw new FenceLogConflict();
        }

        var state = new GrantState(record.Fence.GrantId, 1, null, record.Fence, record.Intents);
        if (!GrantStateValidation.IsKnown(state))
        {
            throw new FenceLogConflict();
        }
    }
}

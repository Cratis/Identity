// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Revocation;

/// <summary>A malformed or incomplete authority document is unknown, never evidence of permission.</summary>
public static class GrantStateValidation
{
    /// <summary>Checks the closed persisted schema before a caller may use it.</summary>
    /// <param name="state">The primary state.</param>
    /// <returns>Whether its denial and outbox invariants are complete.</returns>
    public static bool IsKnown(GrantState state)
    {
        if (!HeaderIsKnown(state) || !state.Outbox.All(entry => EntryIsKnown(state, entry)))
        {
            return false;
        }

        return state.Fence is { } fence ? FencedStateIsKnown(state, fence) : ClearStateIsKnown(state);
    }

    static bool HeaderIsKnown(GrantState state) =>
        state.Version > 0 && state.RecoveryEpoch >= 0 && !string.IsNullOrEmpty(state.GrantId) && state.Outbox is { Length: > 0 } &&
        state.Outbox.All(entry => entry is not null) &&
        state.Outbox.Select(entry => entry.Id).Distinct().Count() == state.Outbox.Length;

    static bool EntryIsKnown(GrantState state, OutboxEntry entry) => EntryIdentityIsKnown(state, entry) && PayloadIsKnown(state, entry);

    static bool EntryIdentityIsKnown(GrantState state, OutboxEntry entry) =>
        Enum.IsDefined(entry.State) && Enum.IsDefined(entry.Kind) && entry.FactId != Guid.Empty &&
        entry.GrantId == OutboxEntry.Source(state.GrantId) && AccountSubject.TryParse(entry.Subject, out _) &&
        entry.Id == $"{entry.FactId:D}:{OutboxEntry.EventTypeId(entry.Kind)}";

    static bool PayloadIsKnown(GrantState state, OutboxEntry entry) => entry.Kind switch
    {
        FactKind.FamilyStarted => FamilyStartIsKnown(entry),
        FactKind.Reuse => RestrictionIsKnown(state, entry) && entry.DetectionId == OutboxEntry.DetectionIdFor(entry.Fence!.Id),
        FactKind.AuthorizationRevocation or FactKind.FamilyRevocation => RestrictionIsKnown(state, entry),
        _ => false
    };

    static bool FamilyStartIsKnown(OutboxEntry entry) =>
        entry.Fence is null && entry.ExpiresAt.HasValue && entry.ExpiresAt > entry.OccurredAt && entry.State != OutboxState.Pending;

    /// <summary>Restriction fact identities are derived from the fence, so a foreign or legacy-derived identity is never evidence.</summary>
    /// <param name="state">The grant state.</param>
    /// <param name="entry">The outbox entry.</param>
    /// <returns>True when the entry is the canonical restriction of the state's fence.</returns>
    static bool RestrictionIsKnown(GrantState state, OutboxEntry entry) =>
        entry.Fence is not null && entry.Fence == state.Fence && entry.FactId == OutboxEntry.RestrictionFactId(entry.Fence.Id, entry.Kind);

    static bool ClearStateIsKnown(GrantState state) =>
        !string.IsNullOrEmpty(state.CurrentRefreshTokenId) && state.Outbox.Length == 1 && state.Outbox[0].Kind == FactKind.FamilyStarted;

    static bool FencedStateIsKnown(GrantState state, GrantFence fence) =>
        state.CurrentRefreshTokenId is null && FenceIdentityIsKnown(state, fence) && RestrictionFactsAreKnown(state, fence);

    static bool FenceIdentityIsKnown(GrantState state, GrantFence fence) =>
        fence.Id != Guid.Empty && fence.Sequence > 0 && fence.GrantId == state.GrantId &&
        AccountSubject.TryParse(fence.Subject, out _) && Enum.IsDefined(fence.Reason);

    static bool RestrictionFactsAreKnown(GrantState state, GrantFence fence) =>
        state.Outbox.Count(entry => entry.Kind == FactKind.AuthorizationRevocation) == 1 &&
        state.Outbox.Count(entry => entry.Kind == FactKind.FamilyRevocation) == 1 &&
        state.Outbox.Count(entry => entry.Kind == FactKind.FamilyStarted) <= 1 &&
        state.Outbox.Count(entry => entry.Kind == FactKind.Reuse) == (fence.Reason == RevocationReason.RefreshTokenReused ? 1 : 0);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation.Events;

namespace Cratis.Identity.Revocation;

/// <summary>Maps the published, Chronicle-free <see cref="OutboxEntry"/> to the internal Chronicle source facts.</summary>
internal static class OutboxEntryFacts
{
    /// <summary>Reconstructs only a closed, token-free fact schema.</summary>
    /// <param name="entry">The persisted intent.</param>
    /// <returns>The internal source fact.</returns>
    /// <exception cref="RevocationPending">The source payload is incomplete or unknown.</exception>
    internal static object Fact(this OutboxEntry entry) => entry.Kind switch
    {
        FactKind.AuthorizationRevocation when entry.Fence is not null => new AuthorizationRevocationRecorded(new(entry.Subject), new(entry.Fence.Reason.ToString()), new(1), new(entry.Fence.Id), new((ulong)entry.Fence.Sequence), new(entry.FactId)),
        FactKind.FamilyRevocation when entry.Fence is not null => new RefreshTokenFamilyRevocationRecorded(new(entry.GrantId), new(entry.Fence.Reason.ToString()), new(entry.Fence.Id), new((ulong)entry.Fence.Sequence), new(entry.FactId)),
        FactKind.FamilyStarted when entry.ExpiresAt.HasValue => new RefreshTokenFamilyStarted(new(entry.GrantId), new(entry.Subject), entry.OccurredAt, entry.ExpiresAt.Value, new(entry.FactId)),
        FactKind.Reuse when entry.DetectionId.HasValue => new RefreshTokenReuseDetected(new(entry.GrantId), new(entry.DetectionId.Value), entry.OccurredAt, new(entry.FactId)),
        _ => throw new RevocationPending()
    };
}

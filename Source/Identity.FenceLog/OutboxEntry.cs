// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Identity.Revocation;

/// <summary>Non-secret, immutable intent; delivery progress is the only mutable field.</summary>
/// <param name="Id">The stable (fact identity, event type) delivery key.</param>
/// <param name="Kind">The allowlisted source fact.</param>
/// <param name="FactId">The durable fact identity.</param>
/// <param name="GrantId">The non-secret UUID used as the event source.</param>
/// <param name="Subject">The account subject.</param>
/// <param name="OccurredAt">The original occurrence.</param>
/// <param name="ExpiresAt">The family's original absolute expiry.</param>
/// <param name="Fence">The exact restriction, if required.</param>
/// <param name="DetectionId">The persisted reuse observation.</param>
/// <param name="State">The durable delivery progress.</param>
public sealed record OutboxEntry(string Id, FactKind Kind, Guid FactId, Guid GrantId, string Subject, DateTimeOffset OccurredAt, DateTimeOffset? ExpiresAt, GrantFence? Fence, Guid? DetectionId, OutboxState State)
{
    /// <summary>Maps opaque operational identifiers to a domain-separated non-secret UUID.</summary>
    /// <param name="grantId">The operational authorization.</param>
    /// <returns>The grant/family UUID.</returns>
    public static Guid Source(string grantId) => new(SHA256.HashData(Encoding.UTF8.GetBytes($"Cratis.Identity.Grant.v1:{grantId}")).AsSpan(0, 16));

    /// <summary>Creates a once-only delivery identity.</summary>
    /// <param name="kind">The closed fact kind.</param>
    /// <param name="grantId">The authorization.</param>
    /// <param name="subject">The account.</param>
    /// <param name="occurred">The occurrence.</param>
    /// <param name="expires">The original expiry.</param>
    /// <param name="fence">The restriction.</param>
    /// <returns>The immutable pending intent.</returns>
    public static OutboxEntry Create(FactKind kind, string grantId, string subject, DateTimeOffset occurred, DateTimeOffset? expires = null, GrantFence? fence = null)
    {
        var factId = fence is null ? Guid.NewGuid() : RestrictionIdentity(fence.Id, $"fact:{EventTypeId(kind)}");
        return new($"{factId:D}:{EventTypeId(kind)}", kind, factId, Source(grantId), subject, occurred, expires, fence, kind == FactKind.Reuse ? RestrictionIdentity(fence!.Id, "reuse-detection") : null, fence is null ? OutboxState.LogConfirmed : OutboxState.Pending);
    }

    /// <summary>Gets the stable event type used in the durable deduplication pair.</summary>
    /// <param name="kind">The source kind.</param>
    /// <returns>The generation-one event identity.</returns>
    /// <exception cref="RevocationPending">The source kind is unknown.</exception>
    public static string EventTypeId(FactKind kind) => kind switch
    {
        FactKind.AuthorizationRevocation => "ac88f9a7-6364-4f20-9301-d533b491b641",
        FactKind.FamilyRevocation => "a80e52ab-051e-4771-bff2-488dccd8aa44",
        FactKind.FamilyStarted => "d9a448be-ccf1-4ab5-b43c-f17dad0ba6c3",
        FactKind.Reuse => "bedbc6d5-2870-4cf7-aa5e-79e7a2bf3cb6",
        _ => throw new RevocationPending()
    };

    static Guid RestrictionIdentity(Guid fenceId, string purpose) => new(SHA256.HashData(Encoding.UTF8.GetBytes($"Cratis.Identity.Restriction.v1:{fenceId:D}:{purpose}")).AsSpan(0, 16));
}

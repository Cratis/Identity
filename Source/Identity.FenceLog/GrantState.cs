// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Identity.Revocation;

/// <summary>The single operational authority for refresh admission and its irreversible tombstone.</summary>
/// <param name="GrantId">The authorization key.</param>
/// <param name="Version">The revision compared by every state transition.</param>
/// <param name="CurrentRefreshTokenId">The only redeemable entry; never included in facts or logs.</param>
/// <param name="Fence">The permanent denial, when present.</param>
/// <param name="Outbox">The immutable delivery intents and their current delivery states.</param>
/// <param name="RecoveryEpoch">The independently held epoch at family creation.</param>
/// <param name="LinkId">The account link the grant was authenticated through; absent on grants written before it was recorded, which are never admitted for refresh.</param>
[SuppressMessage("Design", "CA1819:Properties should not return arrays", Justification = "The persisted document shape of already-written grant state is a compatibility constraint.")]
public sealed record GrantState(string GrantId, long Version, string? CurrentRefreshTokenId, GrantFence? Fence, OutboxEntry[] Outbox, long RecoveryEpoch = 0, Guid? LinkId = null);

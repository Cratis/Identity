// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>The single operational authority for refresh admission and its irreversible tombstone.</summary>
/// <param name="GrantId">The authorization key.</param>
/// <param name="Version">The revision compared by every state transition.</param>
/// <param name="CurrentRefreshTokenId">The only redeemable entry; never included in facts or logs.</param>
/// <param name="Fence">The permanent denial, when present.</param>
/// <param name="Outbox">The immutable delivery intents and their current delivery states.</param>
/// <param name="RecoveryEpoch">The independently held epoch at family creation.</param>
internal sealed record GrantState(string GrantId, long Version, string? CurrentRefreshTokenId, GrantFence? Fence, OutboxEntry[] Outbox, long RecoveryEpoch = 0);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Identity.Revocation;

/// <summary>The complete immutable log entry: the fence and the restriction intents derived from it.</summary>
/// <param name="Fence">The permanent fence.</param>
/// <param name="IntentIds">The identities of the source-fact intents derived from the fence.</param>
/// <param name="Intents">The original immutable payloads, including persisted reuse detection identity.</param>
[SuppressMessage("Design", "CA1819:Properties should not return arrays", Justification = "The persisted JSON shape of already-written logs is a compatibility constraint.")]
public sealed record FenceRecord(GrantFence Fence, IReadOnlyList<string> IntentIds, OutboxEntry[]? Intents = null);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>The complete immutable log entry: the fence and the restriction intents derived from it.</summary>
/// <param name="Fence">The permanent fence.</param>
/// <param name="IntentIds">The identities of the source-fact intents derived from the fence.</param>
internal sealed record FenceRecord(GrantFence Fence, IReadOnlyList<string> IntentIds);

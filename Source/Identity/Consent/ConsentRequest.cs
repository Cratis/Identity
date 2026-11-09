// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow;

/// <summary>An immutable operational snapshot; mutations must compare-and-set its revision.</summary>
/// <param name="Id">The request and evidence idempotency key.</param>
/// <param name="Binding">The immutable authorization parameters.</param>
/// <param name="Deadline">The last instant at which activation is permitted.</param>
/// <param name="State">The durable state.</param>
/// <param name="Revision">The compare-and-set revision.</param>
/// <param name="AuthorizationId">The prepared, operationally guarded authorization.</param>
/// <param name="CodeClaimed">Whether the one-time permission to issue a code has been consumed.</param>
/// <param name="PurgeAt">The terminal-row retention deadline; never set on unresolved requests.</param>
/// <param name="CreatedAt">The immutable creation time from the server clock.</param>
/// <param name="Selection">The validated approval, retained through recovery.</param>
/// <param name="RecoveryEpoch">The immutable creation epoch; legacy rows belong to epoch zero.</param>
internal record ConsentRequest(
    ConsentRequestId Id,
    ConsentBinding Binding,
    DateTimeOffset Deadline,
    ConsentRequestState State = ConsentRequestState.Pending,
    long Revision = 0,
    string? AuthorizationId = null,
    bool CodeClaimed = false,
    DateTimeOffset? PurgeAt = null,
    DateTimeOffset CreatedAt = default,
    ConsentSelection? Selection = null,
    long RecoveryEpoch = 0);

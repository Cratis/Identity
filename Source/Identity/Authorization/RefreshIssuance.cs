// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.Authorization;

/// <summary>Request-scoped proof of the successful grant CAS and OpenIddict redemption.</summary>
internal sealed class RefreshIssuance
{
    /// <summary>Gets the admitted family snapshot, retained even if a later revoke wins.</summary>
    internal GrantState? Admission { get; private set; }

    /// <summary>Gets the grant bound to this response.</summary>
    internal string? GrantId => Admission?.GrantId;

    /// <summary>Gets the preallocated, atomically installed successor.</summary>
    internal string? SuccessorId => Admission?.CurrentRefreshTokenId;

    /// <summary>Records successful admission for this response only.</summary>
    /// <param name="state">The known live family state used by the successful CAS.</param>
    /// <param name="successorId">The successor installed by that CAS.</param>
    internal void Admit(GrantState state, string successorId) => Admission = state with { CurrentRefreshTokenId = successorId };
}

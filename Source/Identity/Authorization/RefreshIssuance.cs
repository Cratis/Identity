// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization;

/// <summary>Request-scoped bridge from the grant CAS to the actual OpenIddict successor insert.</summary>
internal sealed class RefreshIssuance
{
    /// <summary>Gets or sets the grant bound to this response.</summary>
    internal string? GrantId { get; set; }

    /// <summary>Gets or sets the preallocated, atomically installed successor.</summary>
    internal string? SuccessorId { get; set; }
}

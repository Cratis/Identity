// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>The fail-closed default: nobody may request erasure until the policy is decided.</summary>
internal class DenyAllErasureAuthorization : IErasureAuthorization
{
    /// <inheritdoc/>
    // TODO(#51): decide who may request erasure (the person only, or an admin on legal request, SLA, tombstoned/disabled accounts).
    public Task<bool> IsAllowed(ClaimsPrincipal requester, AccountSubject account) => Task.FromResult(false);
}

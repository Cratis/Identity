// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>The fail-closed default: no boundary is approved, so erasure can never be reported complete.</summary>
internal class UnconfiguredErasureBoundaryPolicy : IErasureBoundaryPolicy
{
    /// <inheritdoc/>
    // TODO: backups/restore retention, export retention, Ante's erasure/acknowledgement contract and the
    // consumer acknowledgement policy must be decided and configured explicitly before a boundary is approved.
    public BoundaryVersion? Approved => null;
}

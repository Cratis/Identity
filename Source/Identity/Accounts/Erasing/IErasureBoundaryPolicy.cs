// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>The approved completion boundary: backups/restore, exports and consumer acknowledgement policy.</summary>
internal interface IErasureBoundaryPolicy
{
    /// <summary>Gets the approved boundary version, or null while no boundary is approved and configured.</summary>
    BoundaryVersion? Approved { get; }
}

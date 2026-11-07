// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>The authoritative decision of a consume-and-install CAS.</summary>
internal enum RedeemOutcome
{
    /// <summary>Both consumption and successor admission committed.</summary>
    Installed = 0,

    /// <summary>A permanent restriction won.</summary>
    Fenced = 1,

    /// <summary>The presented entry is no longer current.</summary>
    NotCurrent = 2,

    /// <summary>The caller must re-read before deciding.</summary>
    VersionConflict = 3,

    /// <summary>No provable state; deny.</summary>
    Unknown = 4
}

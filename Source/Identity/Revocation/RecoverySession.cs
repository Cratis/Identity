// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>Per-host identity; durable custody, not these flags, is the source of truth.</summary>
internal sealed class RecoverySession
{
    /// <summary>Gets the host incarnation used for leased recovery.</summary>
    internal string Owner { get; } = Guid.NewGuid().ToString("N");

    /// <summary>Gets or sets a value indicating whether startup verified the deployment boundary.</summary>
    internal bool Verified { get; set; }

    /// <summary>Gets or sets a value indicating whether Chronicle custody was bound.</summary>
    internal bool Bound { get; set; }

    /// <summary>Gets or sets a value indicating whether this host consumed its configured declaration.</summary>
    internal bool DeclarationConsumed { get; set; }

    /// <summary>Gets or sets the in-progress unique declaration.</summary>
    internal string? RestoreId { get; set; }

    /// <summary>Gets or sets the consumers being restored.</summary>
    internal RestoreKind Kind { get; set; }

    /// <summary>Gets or sets a value indicating whether the independent epoch was allocated.</summary>
    internal bool Registered { get; set; }
}

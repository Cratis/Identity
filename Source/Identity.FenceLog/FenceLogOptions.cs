// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>Operator-owned independent custody and restore declaration settings, bound from the Identity:FenceLog configuration section.</summary>
public sealed class FenceLogOptions
{
    /// <summary>Gets or sets the independently provisioned connection.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets the independent database name.</summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>Gets or sets the externally recorded custody identity.</summary>
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>Gets or sets the externally observed minimum allocation bound.</summary>
    public long? MinimumSequence { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the fence log may live in the same deployment (the same replica set) as the operational store.
    /// </summary>
    /// <remarks>
    /// Off by default. Turning it on is the operator's attestation that backups and restores are made per database, that the fence log
    /// database is backed up on its own, and that it is never restored together with, or to an earlier point than, the operational
    /// database. The database must still differ from the operational one. A whole-deployment rollback (a snapshot of the server, a
    /// point-in-time recovery of the replica set) would roll the fence log back with the data it guards, so Identity cannot detect it.
    /// </remarks>
    public bool AllowSharedDeployment { get; set; }

    /// <summary>Gets or sets the unique opaque restore declaration.</summary>
    public string RestoreId { get; set; } = string.Empty;

    /// <summary>Gets or sets the consumers being restored.</summary>
    public RestoreKind RestoreKind { get; set; }

    /// <summary>Gets or sets a value indicating whether independent reconciliation proved every uncovered restriction.</summary>
    public bool RestrictionsReestablished { get; set; }
}

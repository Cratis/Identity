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

    /// <summary>Gets or sets the unique opaque restore declaration.</summary>
    public string RestoreId { get; set; } = string.Empty;

    /// <summary>Gets or sets the consumers being restored.</summary>
    public RestoreKind RestoreKind { get; set; }

    /// <summary>Gets or sets a value indicating whether independent reconciliation proved every uncovered restriction.</summary>
    public bool RestrictionsReestablished { get; set; }
}

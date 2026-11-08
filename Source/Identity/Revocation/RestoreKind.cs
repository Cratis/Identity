// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>The consumer stores named by a unique restore declaration.</summary>
internal enum RestoreKind
{
    /// <summary>No declared restore.</summary>
    None = 0,

    /// <summary>The operational grant store was restored.</summary>
    Operational = 1,

    /// <summary>Chronicle was restored.</summary>
    Chronicle = 2,

    /// <summary>Both consumers were restored.</summary>
    Both = 3
}

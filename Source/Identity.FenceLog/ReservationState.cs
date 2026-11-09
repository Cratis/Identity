// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Identity.Revocation;

/// <summary>Only reconciliation may leave Fencing after operational cancellation or committed-fence proof.</summary>
public enum ReservationState
{
    /// <summary>An allocated writer intent.</summary>
    [SuppressMessage("Naming", "CA1700:Do not name enum values 'Reserved'", Justification = "The persisted numeric state and name are a compatibility constraint.")]
    Reserved = 0,

    /// <summary>Reconciliation owns the log-side writer fence.</summary>
    Fencing = 1,

    /// <summary>A complete permanent denial.</summary>
    Filled = 2,

    /// <summary>A proven operational cancellation.</summary>
    Voided = 3,

    /// <summary>An independently durable recovery epoch fence and publication intent.</summary>
    Epoch = 4
}

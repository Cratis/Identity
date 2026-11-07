// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Operational;

/// <summary>A legacy account store could not be checked. The message is fixed and never carries driver text.</summary>
/// <param name="reason">Why the check failed.</param>
/// <param name="innerException">The driver failure, kept for in-process diagnosis only.</param>
public sealed class LegacyStoreProbeFailed(LegacyStoreFailure reason, Exception? innerException)
    : Exception("The legacy account store could not be checked.", innerException)
{
    /// <summary>Gets why the check failed.</summary>
    public LegacyStoreFailure Reason { get; } = reason;
}

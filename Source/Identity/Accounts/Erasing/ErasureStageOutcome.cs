// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>The result of one stage; a failure carries only an allowlisted reason.</summary>
/// <param name="Reason">The failure reason, or null when the stage succeeded.</param>
internal record ErasureStageOutcome(ReasonId? Reason)
{
    /// <summary>Gets whether the stage completed.</summary>
    internal bool Succeeded => Reason is null;

    /// <summary>Gets the successful outcome.</summary>
    internal static ErasureStageOutcome Success { get; } = new((ReasonId?)null);

    /// <summary>Creates a failed outcome.</summary>
    /// <param name="reason">The allowlisted reason.</param>
    /// <returns>The outcome.</returns>
    internal static ErasureStageOutcome Failure(ReasonId reason) => new(reason);
}

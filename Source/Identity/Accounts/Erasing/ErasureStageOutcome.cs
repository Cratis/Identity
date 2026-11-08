// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>The result of one stage; a failure carries only an allowlisted reason.</summary>
/// <param name="Reason">The failure reason, or null when the stage succeeded.</param>
/// <param name="Covered">The namespaces the stage reports it handled; the coordinator requires every scoped namespace.</param>
internal record ErasureStageOutcome(ReasonId? Reason, IReadOnlyList<string> Covered)
{
    /// <summary>Gets whether the stage completed.</summary>
    internal bool Succeeded => Reason is null;

    /// <summary>Creates a successful outcome that covered the given namespaces.</summary>
    /// <param name="covered">The namespaces handled.</param>
    /// <returns>The outcome.</returns>
    internal static ErasureStageOutcome Success(IReadOnlyList<string> covered) => new(null, covered);

    /// <summary>Creates a failed outcome.</summary>
    /// <param name="reason">The allowlisted reason.</param>
    /// <returns>The outcome.</returns>
    internal static ErasureStageOutcome Failure(ReasonId reason) => new(reason, []);
}

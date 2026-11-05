// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.Conformance;

/// <summary>
/// Known protocol gaps, never MUST-level security failures.
/// </summary>
public static class ExpectedFailures
{
    public static IReadOnlyList<ExpectedFailure> All { get; } = [];

    /// <summary>
    /// An explicitly owned gap in conformance.
    /// </summary>
    /// <param name="Requirement">The requirement identifier.</param>
    /// <param name="Reason">Why the requirement is not yet met.</param>
    /// <param name="Issue">The follow-up issue URL.</param>
    public record ExpectedFailure(string Requirement, string Reason, string Issue);
}

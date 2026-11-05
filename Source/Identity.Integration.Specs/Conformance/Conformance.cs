// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.Conformance;

/// <summary>
/// Runs assertions with strict expected-failure semantics.
/// </summary>
public static class Conformance
{
    /// <summary>
    /// Skips a documented failure, but fails if that requirement now conforms.
    /// </summary>
    /// <param name="requirement">The requirement identifier.</param>
    /// <param name="assertion">The protocol assertion, excluding fixture setup.</param>
    public static void Expect(string requirement, Action assertion)
    {
        var failure = ExpectedFailures.All.SingleOrDefault(_ => _.Requirement == requirement);
        if (failure is null)
        {
            assertion();
            return;
        }

        if (Record.Exception(assertion) is not null)
        {
            Assert.Skip($"Expected failure {requirement}: {failure.Reason} ({failure.Issue})");
        }

        Assert.Fail($"{requirement} now conforms; remove it from ExpectedFailures.");
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_ConformanceCatalog;

/// <summary>
/// Reports the known gap, without claiming to exercise signing-key rotation.
/// </summary>
[Requirement(Requirements.KeyRotationOverlap)]
public class when_reporting_unproven_key_rotation
{
    [Fact]
    void should_report_the_unproven_overlap_requirement()
    {
        var failure = ExpectedFailures.All.Single(_ => _.Requirement == Requirements.KeyRotationOverlap);
        Assert.Skip($"Expected failure {failure.Requirement}: {failure.Reason} ({failure.Issue})");
    }
}

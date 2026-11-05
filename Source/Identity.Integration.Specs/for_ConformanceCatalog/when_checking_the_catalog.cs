// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Identity.Integration.for_ConformanceCatalog;

[Trait("Category", "Conformance")]
public class when_checking_the_catalog
{
    static readonly Type[] _specTypes = typeof(when_checking_the_catalog).Assembly.GetTypes();
    static readonly RequirementAttribute[] _tags = _specTypes.SelectMany(_ => _.GetCustomAttributes<RequirementAttribute>()).ToArray();

    /// <summary>
    /// Gets requirements with no runnable proof, reported individually rather than silently counted as covered.
    /// </summary>
    public static TheoryData<string, string, string> UnprovenRequirements => new(
        ExpectedFailures.All.Where(failure => !_tags.Any(_ => _.Id == failure.Requirement))
            .Select(_ => (_.Requirement, _.Reason, _.Issue)));

    [Fact] void should_cover_every_requirement_with_a_spec_or_an_expected_failure() => Requirements.All.All(requirement => _tags.Any(_ => _.Id == requirement.Id) || ExpectedFailures.All.Any(_ => _.Requirement == requirement.Id)).ShouldBeTrue();
    [Fact] void should_only_tag_catalogued_requirements() => _tags.All(tag => Requirements.All.Any(_ => _.Id == tag.Id)).ShouldBeTrue();
    [Fact] void should_only_list_catalogued_requirements_as_expected_failures() => ExpectedFailures.All.All(failure => Requirements.All.Any(_ => _.Id == failure.Requirement)).ShouldBeTrue();
    [Fact] void should_give_every_expected_failure_a_reason_and_an_issue() => ExpectedFailures.All.All(HasReasonAndIssue).ShouldBeTrue();
    [Fact] void should_not_skip_conformance_specs_statically() => _specTypes.Where(IsConformanceSpec).SelectMany(_ => _.GetMethods()).SelectMany(_ => _.GetCustomAttributes<FactAttribute>()).Any(_ => _.Skip is not null).ShouldBeFalse();
    [Fact] void should_have_unique_requirement_ids() => Requirements.All.Select(_ => _.Id).Distinct(StringComparer.Ordinal).Count().ShouldEqual(Requirements.All.Count);
    [Fact] void should_have_unique_expected_failures() => ExpectedFailures.All.Select(_ => _.Requirement).Distinct(StringComparer.Ordinal).Count().ShouldEqual(ExpectedFailures.All.Count);
    [Fact] void should_not_hide_must_level_failures() => ExpectedFailures.All.Any(failure => Requirements.All.Any(_ => _.Id == failure.Requirement && _.Level == "MUST")).ShouldBeFalse();
    [Fact] void should_tag_classes_that_have_facts() => _specTypes.Where(_ => _.IsDefined(typeof(RequirementAttribute))).All(_ => _.GetMethods().Any(method => method.IsDefined(typeof(FactAttribute)))).ShouldBeTrue();

    [Theory]
    [MemberData(nameof(UnprovenRequirements))]
    public void should_report_each_unproven_requirement(string requirement, string reason, string issue) =>
        Assert.Skip($"Expected failure {requirement}: {reason} ({issue})");

    static bool HasReasonAndIssue(ExpectedFailures.ExpectedFailure failure) =>
        !string.IsNullOrWhiteSpace(failure.Reason) && Uri.TryCreate(failure.Issue, UriKind.Absolute, out var issue) &&
        issue.Scheme == Uri.UriSchemeHttps && issue.AbsolutePath.Contains("/issues/", StringComparison.Ordinal);

    static bool IsConformanceSpec(Type type) => type.IsDefined(typeof(RequirementAttribute)) || type.GetCustomAttributes<TraitAttribute>().Any(_ => _.Name == "Category" && _.Value == "Conformance");
}

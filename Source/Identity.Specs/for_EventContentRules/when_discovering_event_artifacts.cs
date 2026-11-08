// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Concepts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Contracts.Check;
using Cratis.Identity.Contracts.Compliance;

namespace Cratis.Identity.for_EventContentRules;

public class when_discovering_event_artifacts : Specification
{
    [Fact]
    public void should_check_the_real_contracts()
    {
        using var output = new StringWriter();
        CheckRunner.Run(ReflectionShapeBuilder.Discover(typeof(AccountRegistered).Assembly), output).ShouldEqual(0);
    }

    [Fact]
    public void should_discover_nonpublic_and_previous_generations()
    {
        var types = ReflectionShapeBuilder.Discover(typeof(when_discovering_event_artifacts).Assembly, [typeof(ExplicitArtifact)]);
        types.ShouldContain(typeof(InternalArtifact));
        types.ShouldContain(typeof(PreviousArtifact));
        types.ShouldContain(typeof(ExplicitArtifact));
    }

    [Fact]
    public void should_fail_the_runner_for_violating_artifacts()
    {
        using var output = new StringWriter();
        CheckRunner.Run([typeof(InternalArtifact)], output).ShouldEqual(1);
        output.ToString().Contains("forbidden_member_name", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    public void should_fail_empty_discovery()
    {
        using var output = new StringWriter();
        CheckRunner.Run([], output).ShouldEqual(1);
    }

    [Theory]
    [InlineData(typeof(PropertyPii))]
    [InlineData(typeof(ParameterPii))]
    [InlineData(typeof(ConceptPii))]
    [InlineData(typeof(TypePii))]
    [InlineData(typeof(InheritedPii))]
    public void should_resolve_chronicle_pii_metadata(Type type) => ContentRules.Check(ReflectionShapeBuilder.Build(type, true), SafeConceptRegistry.Entries).ShouldBeEmpty();

    [Fact]
    public void should_reject_public_subjects_even_with_pii() => ContentRules.Check(ReflectionShapeBuilder.Build(typeof(PublicSubject), true), SafeConceptRegistry.Entries)
        .ShouldContain(new Violation("IdpSub", "public_upstream_identifier"));

    [Fact]
    public void should_require_an_account_subject_for_raw_subjects() => ContentRules.Check(ReflectionShapeBuilder.Build(typeof(ProtectedSubject)), SafeConceptRegistry.Entries)
        .ShouldContain(new Violation("IdpSub", "unresolved_compliance_subject"));

    [Fact]
    public void should_traverse_collections_and_nullable_wrappers() => ContentRules.Check(ReflectionShapeBuilder.Build(typeof(NestedMembers)), SafeConceptRegistry.Entries)
        .ShouldContain(new Violation("Details.[].Password", "forbidden_member_name"));

    [Fact]
    public void should_not_treat_an_unregistered_concept_as_safe() => ContentRules.Check(ReflectionShapeBuilder.Build(typeof(UnreviewedDigest)), SafeConceptRegistry.Entries)
        .ShouldContain(new Violation("KeyHash", "unapproved_safe_concept"));

    [EventType("7d7ed4c6-eb9d-43ad-a2a8-46a99f1d2c5c")]
    internal sealed record InternalArtifact(string AccessToken);
    [EventTypeGenerationFor<InternalArtifact>(2)]
    internal sealed record PreviousArtifact(string Password);
    internal sealed record ExplicitArtifact(string ClientSecret);
    internal sealed record PropertyPii([property: PII] string Email);
    internal sealed record ParameterPii([PII] string Email);
    internal sealed record ConceptPii(ProtectedText Email);
    [PII]
    internal sealed record ProtectedText(string Value) : ConceptAs<string>(Value);
    [PII]
    internal sealed record TypePii(string Email);
    internal record BasePii([property: PII] string Email);
    internal sealed record InheritedPii(string Email) : BasePii(Email);
    [Public]
    internal sealed record PublicSubject([property: PII] string IdpSub);
    internal sealed record ProtectedSubject([property: PII] string IdpSub);
    internal sealed record NestedMembers(List<Nested?> Details);
    internal sealed record Nested(string Password);
    internal sealed record UnreviewedDigest(Unreviewed KeyHash);
    internal sealed record Unreviewed(string Value) : ConceptAs<string>(Value);
}

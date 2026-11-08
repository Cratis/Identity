// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Contracts.Check;
using Cratis.Identity.Contracts.Compliance;

namespace Cratis.Identity.for_EventContentRules;

public class when_checking_safe_concepts : Specification
{
    [Fact]
    public void should_accept_the_reviewed_public_lookup_digest() => ContentRules.Check(Shape(), SafeConceptRegistry.Entries).ShouldBeEmpty();

    [Fact]
    public void should_reject_a_different_underlying_type() => Check(Entry() with { UnderlyingType = "Guid" }).ShouldNotBeEmpty();

    [Fact]
    public void should_require_a_producer_specification() => Check(Entry() with { ProducerSpec = string.Empty }).ShouldNotBeEmpty();

    [Fact]
    public void should_require_a_justification() => Check(Entry() with { Justification = string.Empty }).ShouldNotBeEmpty();

    [Fact]
    public void should_require_a_keyed_upstream_digest() => Check(Entry() with { Keyed = false }).ShouldNotBeEmpty();

    [Fact]
    public void should_require_a_purpose() => Check(Entry() with { Purpose = null }).ShouldNotBeEmpty();

    [Fact]
    public void should_require_a_revision() => Check(Entry() with { HashKeyRevision = null }).ShouldNotBeEmpty();

    [Fact]
    public void should_reject_a_redeemable_reference() => Check(Entry() with { Redeemable = true }).ShouldNotBeEmpty();

    [Fact]
    public void should_require_pii_for_the_digest() => ContentRules.Check(Shape() with { Members = [Shape().Members[0] with { Pii = false }] }, SafeConceptRegistry.Entries).ShouldNotBeEmpty();

    [Fact]
    public void should_require_the_account_subject() => ContentRules.Check(Shape() with { ComplianceSubject = false }, SafeConceptRegistry.Entries).ShouldNotBeEmpty();

    [Fact]
    public void should_not_bless_nested_content() => ContentRules.Check(Shape() with { Members = [Shape().Members[0] with { Members = [Shape().Members[0]] }] }, SafeConceptRegistry.Entries).ShouldNotBeEmpty();

    [Fact]
    public void should_not_bless_a_byte_buffer() => ContentRules.Check(Shape() with { Members = [Shape().Members[0] with { ForbiddenType = true }] }, SafeConceptRegistry.Entries).ShouldNotBeEmpty();

    [Fact]
    public void should_not_bless_an_open_shape() => ContentRules.Check(Shape() with { Members = [Shape().Members[0] with { OpenShape = true }] }, SafeConceptRegistry.Entries).ShouldNotBeEmpty();

    static EventShape Shape()
    {
        var shape = ReflectionShapeBuilder.Build(typeof(UpstreamKeyLinked), true);
        return shape with { Members = [shape.Members.Single(member => member.ClrName == "KeyHash")] };
    }

    static SafeConceptEntry Entry() => SafeConceptRegistry.Entries[typeof(UpstreamLookupDigest).FullName];
    static IReadOnlyList<Violation> Check(SafeConceptEntry entry) => ContentRules.Check(Shape(), new Dictionary<string, SafeConceptEntry> { [typeof(UpstreamLookupDigest).FullName] = entry });
}

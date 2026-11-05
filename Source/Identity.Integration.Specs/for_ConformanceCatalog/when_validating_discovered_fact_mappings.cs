// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Identity.Integration.for_Conformance;

namespace Cratis.Identity.Integration.for_ConformanceCatalog;

[Trait("Category", "Conformance")]
public class when_validating_discovered_fact_mappings
{
    static readonly IReadOnlyList<(string Requirement, MethodInfo Fact)> _facts = CatalogFacts.Tagged([typeof(when_redeeming_the_same_authorization_code_twice)]);

    [Fact] void should_find_the_private_tagged_facts() => _facts.Count.ShouldEqual(3);
    [Fact] void should_discover_non_public_methods() => _facts.All(_ => _.Fact.IsPrivate).ShouldBeTrue();
    [Fact] void should_accept_a_requirement_with_discovered_facts() => CatalogFacts.Covers([Requirements.CodeSingleUse], _facts).ShouldBeTrue();
    [Fact] void should_reject_a_requirement_without_a_tagged_fact() => CatalogFacts.Covers([Requirements.CodeSingleUse, Requirements.CodeRedirectBound], _facts).ShouldBeFalse();
    [Fact] void should_reject_a_tagged_fact_with_an_unknown_requirement() => CatalogFacts.OnlyKnown([Requirements.CodeRedirectBound], _facts).ShouldBeFalse();
    [Fact] void should_accept_known_fact_mappings() => CatalogFacts.OnlyKnown([Requirements.CodeSingleUse], _facts).ShouldBeTrue();
    [Fact] void should_reject_empty_fact_discovery() => CatalogFacts.Covers([Requirements.CodeSingleUse], []).ShouldBeFalse();
    [Fact] void should_reject_vacuous_coverage_of_an_empty_catalogue() => CatalogFacts.Covers([], []).ShouldBeFalse();

    [Theory]
    [InlineData(nameof(should_discover_theories_as_derived_fact_attributes))]
    public void should_discover_theories_as_derived_fact_attributes(string name) => CatalogFacts.DeclaredBy(typeof(when_validating_discovered_fact_mappings)).Select(_ => _.Name).ShouldContain(name);
}

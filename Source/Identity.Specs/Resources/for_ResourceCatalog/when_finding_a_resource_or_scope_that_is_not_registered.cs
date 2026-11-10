// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Resources.for_ResourceCatalog;

public class when_finding_a_resource_or_scope_that_is_not_registered : given.a_resource_catalog
{
    ResourceServer _unknownIndicator;
    ResourceServer _malformedIndicator;
    ResourceServer _fragmentIndicator;
    ResourceServer _unknownAudience;
    string[] _owners;
    bool _unknownScope;
    bool _differentCaseScope;

    async Task Because()
    {
        _unknownIndicator = Resources.FindByIndicator("https://unknown.example/mcp");
        _malformedIndicator = Resources.FindByIndicator("not a uri");
        _fragmentIndicator = Resources.FindByIndicator("https://cratis.direct/mcp#fragment");
        _unknownAudience = Resources.FindByAudience("unknown-api");
        _owners = Resources.AudiencesOwning("unknown:scope");
        _unknownScope = await Resources.HasScope("unknown:scope");
        _differentCaseScope = await Resources.HasScope("DIRECT:READ");
    }

    [Fact] void should_not_find_an_unknown_indicator() => _unknownIndicator.ShouldBeNull();
    [Fact] void should_not_find_a_malformed_indicator() => _malformedIndicator.ShouldBeNull();
    [Fact] void should_not_find_an_indicator_with_a_fragment() => _fragmentIndicator.ShouldBeNull();
    [Fact] void should_not_find_an_unknown_audience() => _unknownAudience.ShouldBeNull();
    [Fact] void should_have_no_owner_for_an_unknown_scope() => _owners.ShouldBeEmpty();
    [Fact] void should_not_know_an_unknown_scope() => _unknownScope.ShouldBeFalse();
    [Fact] void should_not_match_scope_names_case_insensitively() => _differentCaseScope.ShouldBeFalse();
}

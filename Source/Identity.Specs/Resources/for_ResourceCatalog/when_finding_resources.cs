// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;

namespace Cratis.Identity.Resources.for_ResourceCatalog;

public class when_finding_resources : given.a_resource_catalog
{
    ResourceServer _byIndicator;
    ResourceServer _byNormalizedIndicator;
    ResourceServer _byAudience;
    string[] _owners;

    void Because()
    {
        _byIndicator = Resources.FindByIndicator(Configurations.DirectMcp);
        _byNormalizedIndicator = Resources.FindByIndicator("HTTPS://CRATIS.DIRECT/mcp");
        _byAudience = Resources.FindByAudience(Configurations.StudioAudience);
        _owners = Resources.AudiencesOwning("direct:work");
    }

    [Fact] void should_find_the_resource_by_indicator() => _byIndicator.Audience.ShouldEqual(Configurations.DirectAudience);
    [Fact] void should_normalize_the_requested_indicator() => _byNormalizedIndicator.Audience.ShouldEqual(Configurations.DirectAudience);
    [Fact] void should_find_the_resource_by_audience() => _byAudience.Audience.ShouldEqual(Configurations.StudioAudience);
    [Fact] void should_keep_the_display_name() => _byAudience.DisplayName.ShouldEqual("Cratis Studio");
    [Fact] void should_flag_only_the_declared_upstream_identity_consumer() => (Resources.FindByAudience(Configurations.DirectAudience).ReceivesUpstreamIdentity, _byAudience.ReceivesUpstreamIdentity).ShouldEqual((true, false));
    [Fact] void should_find_the_owner_of_a_scope() => _owners.ShouldContainOnly(Configurations.DirectAudience);
}

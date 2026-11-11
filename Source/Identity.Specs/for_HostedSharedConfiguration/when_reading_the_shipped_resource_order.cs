// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cratis.Identity.for_HostedSharedConfiguration;

/// <summary>
/// Pins the shipped catalog order: the Infrastructure realm addresses resources by index (Identity__Resources__N__*),
/// so reordering silently retargets those settings.
/// </summary>
public class when_reading_the_shipped_resource_order : Specification
{
    IdentityOptions _options;

    void Establish()
    {
        var configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("CratisId.appsettings.json").Build();
        _options = new IdentityOptions();
        configuration.GetSection("Identity").Bind(_options);
    }

    [Fact] void should_have_exactly_three_resources() => _options.Resources.Count.ShouldEqual(3);
    [Fact] void should_keep_the_direct_v1_audience_at_index_zero() => _options.Resources[0].Audience.ShouldEqual("direct-v1");
    [Fact] void should_keep_the_direct_mcp_audience_at_index_one() => _options.Resources[1].Audience.ShouldEqual("direct-mcp");
    [Fact] void should_keep_the_studio_audience_at_index_two() => _options.Resources[2].Audience.ShouldEqual("studio-mcp");
    [Fact] void should_ship_studio_without_an_indicator() => _options.Resources[2].Indicators.ShouldBeEmpty();
    [Fact] void should_ship_the_studio_scopes() => _options.Resources[2].Scopes.Select(scope => scope.Name).SequenceEqual(["studio:mcp:read", "studio:mcp:write"]).ShouldBeTrue();
}

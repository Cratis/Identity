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

    [Fact] void should_have_exactly_two_resources() => _options.Resources.Count.ShouldEqual(2);
    [Fact] void should_keep_the_direct_v1_audience_at_index_zero() => _options.Resources[0].Audience.ShouldEqual("direct-v1");
    [Fact] void should_keep_the_direct_api_audience_at_index_one() => _options.Resources[1].Audience.ShouldEqual("direct-api");
}

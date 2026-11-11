// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Development;
using Microsoft.Extensions.Configuration;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_reading_the_shipped_development_settings : Specification
{
    DevelopmentOptions _options;
    string[] _audiences;

    void Because()
    {
        var configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("Identity.appsettings.Development.json").Build();
        _options = configuration.GetSection($"{IdentityOptions.SectionName}:{DevelopmentOptions.SectionName}").Get<DevelopmentOptions>();
        _audiences = [.. _options.Resources.Select(_ => _.Audience)];
    }

    [Fact] void should_use_the_single_direct_mcp_audience_constant() => _audiences.ShouldContain(DevelopmentProfile.DirectMcpAudience);
    [Fact] void should_not_register_the_rest_audience() => _audiences.ShouldNotContain(DevelopmentProfile.DirectApiAudience);
    [Fact] void should_keep_the_flag_off_so_the_profile_is_opt_in() => _options.Enabled.ShouldBeFalse();
}

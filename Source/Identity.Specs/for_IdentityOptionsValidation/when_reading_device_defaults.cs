// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_reading_device_defaults : Specification
{
    IdentityOptions _defaults;
    IdentityOptions _bound;

    void Because()
    {
        _defaults = new();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Identity:Device:Enabled"] = "true",
            ["Identity:Device:AttemptsGlobally"] = "321"
        }).Build();
        _bound = configuration.GetSection(IdentityOptions.SectionName).Get<IdentityOptions>();
    }

    [Fact] void should_require_explicit_opt_in() => _defaults.DeviceGrant.Enabled.ShouldBeFalse();
    [Fact] void should_expire_codes_after_five_minutes() => _defaults.DeviceGrant.CodeLifetime.ShouldEqual(TimeSpan.FromMinutes(5));
    [Fact] void should_poll_at_five_seconds() => _defaults.DeviceGrant.PollingInterval.ShouldEqual(TimeSpan.FromSeconds(5));
    [Fact] void should_bind_the_planned_device_section() => _bound.DeviceGrant.Enabled.ShouldBeTrue();
    [Fact] void should_bind_the_global_ceiling() => _bound.DeviceGrant.AttemptsGlobally.ShouldEqual(321);
}

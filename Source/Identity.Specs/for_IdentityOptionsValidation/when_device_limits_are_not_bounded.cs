// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_device_limits_are_not_bounded : Specification
{
    bool _baseline;
    ValidateOptionsResult[] _results;

    void Because()
    {
        var validator = new IdentityOptionsValidation(new HostingEnvironment { EnvironmentName = "Development" });
        _baseline = validator.Validate(null, Configurations.TwoResourceServers()).Succeeded;
        Action<DeviceGrantOptions>[] changes =
        [
            _ => _.CodeLifetime = TimeSpan.FromMinutes(16),
            _ => _.CodeLifetime = TimeSpan.Zero,
            _ => _.PollingInterval = TimeSpan.FromSeconds(4),
            _ => _.PollingInterval = TimeSpan.FromMilliseconds(5500),
            _ => _.AttemptsPerCode = 11,
            _ => _.AttemptsPerAccount = 0,
            _ => _.AttemptsPerIp = 301,
            _ => _.VerificationWindow = TimeSpan.FromHours(2),
            _ => _.AttemptsGlobally = 1001,
            _ => _.IssuancesPerIp = 0,
            _ => _.UserCodeLength = 7,
            _ => _.UserCodeCharset = "BCDFGHJKLMNPQRSTVWX",
            _ => _.UserCodeCharset = "BCDFGHJKLMNPQRSTVWXX"
        ];
        _results = changes.Select(change =>
        {
            var options = Configurations.TwoResourceServers();
            options.DeviceGrant.Enabled = true;
            change(options.DeviceGrant);
            return validator.Validate(null, options);
        }).ToArray();
    }

    [Fact] void should_accept_the_defaults() => _baseline.ShouldBeTrue();
    [Fact] void should_reject_each_unbounded_or_invalid_device_limit() => _results.All(_ => _.Failed).ShouldBeTrue();
    [Fact] void should_exercise_all_limits() => _results.Length.ShouldEqual(13);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_the_reload_interval_is_out_of_range : Specification
{
    List<ValidateOptionsResult> _results;

    void Because()
    {
        var validation = new IdentityOptionsValidation(new HostingEnvironment { EnvironmentName = Environments.Development });
        _results = new[] { TimeSpan.Zero, TimeSpan.FromMilliseconds(999), TimeSpan.FromMinutes(5.1) }.Select(interval =>
        {
            var options = Configurations.TwoResourceServers();
            options.SecretStore.ReloadInterval = interval;
            return validation.Validate(null, options);
        }).ToList();
    }

    [Fact] void should_refuse_every_interval_outside_one_second_to_five_minutes() => _results.TrueForAll(_ => _.Failed).ShouldBeTrue();
}

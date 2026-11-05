// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_the_reload_interval_is_at_the_limits : Specification
{
    List<ValidateOptionsResult> _results;

    void Because()
    {
        var validation = new IdentityOptionsValidation(new HostingEnvironment { EnvironmentName = Environments.Development });
        _results = new[] { TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5) }.Select(interval =>
        {
            var options = Configurations.TwoResourceServers();
            options.SecretStore.ReloadInterval = interval;
            return validation.Validate(null, options);
        }).ToList();
    }

    [Fact] void should_accept_them() => _results.TrueForAll(_ => _.Succeeded).ShouldBeTrue();
}

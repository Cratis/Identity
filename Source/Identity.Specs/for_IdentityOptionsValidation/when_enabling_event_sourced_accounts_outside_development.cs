// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_enabling_event_sourced_accounts_outside_development : Specification
{
    ValidateOptionsResult _result;

    void Because()
    {
        var options = Configurations.TwoResourceServers();
        options.Accounts.EventSourced = true;
        _result = new IdentityOptionsValidation(new HostingEnvironment { EnvironmentName = Environments.Production }).Validate(null, options);
    }

    [Fact] void should_refuse_the_development_only_path() => _result.Failures.ShouldContain("Event-sourced accounts are Development-only, pending owner decision D18.");
}

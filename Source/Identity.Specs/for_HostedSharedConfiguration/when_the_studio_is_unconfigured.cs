// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Hosting;
using Cratis.Identity.Resources;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_HostedSharedConfiguration;

public class when_the_studio_is_unconfigured : given.a_hosted_studio_configuration
{
    protected override string? StudioUrl => null;

    protected override string? StudioSecret => null;

    ValidateOptionsResult _result;
    IdentityOptions _options;
    ResourceCatalog _catalog;

    void Because()
    {
        _result = Validate();
        _options = Bound();
        var validator = new HostedSharedConfiguration(_configuration, new HostingEnvironment { EnvironmentName = Environments.Production });
        HostedSharedConfiguration.Remove(_options, validator.DisabledAudiences(_options));
        _catalog = new ResourceCatalog(Options.Create(_options));
    }

    [Fact] void should_accept_the_configuration() => _result.Succeeded.ShouldBeTrue();
    [Fact] void should_leave_the_two_direct_resources() => _catalog.All.Count().ShouldEqual(2);
    [Fact] void should_not_know_the_studio_audience() => _catalog.FindByAudience("studio-mcp").ShouldBeNull();
    [Fact] void should_not_own_the_studio_scopes() => _catalog.FindByScope("studio:mcp:read").ShouldBeEmpty();
    [Fact] void should_pass_the_general_validation_without_the_studio() => new IdentityOptionsValidation(new HostingEnvironment { EnvironmentName = Environments.Production }).Validate(null, _options).Failed.ShouldBeFalse();
}

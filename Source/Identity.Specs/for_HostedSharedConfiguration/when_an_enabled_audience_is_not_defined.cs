// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Cratis.Identity.Configuration;
using Cratis.Identity.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_HostedSharedConfiguration;

public class when_an_enabled_audience_is_not_defined : Specification
{
    ValidateOptionsResult _result;

    void Because()
    {
        var configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("CratisId.appsettings.json")
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Identity:Hosting:EnabledAudiences:0"] = "typo-mcp" }).Build();
        var options = new IdentityOptions();
        configuration.GetSection("Identity").Bind(options);
        _result = new HostedSharedConfiguration(configuration, new HostingEnvironment { EnvironmentName = Environments.Production }).Validate(null, options);
    }

    [Fact] void should_refuse_the_configuration() => _result.Failed.ShouldBeTrue();
    [Fact] void should_name_the_audience() => _result.Failures.ShouldContain(failure => failure.Contains("typo-mcp"));
}

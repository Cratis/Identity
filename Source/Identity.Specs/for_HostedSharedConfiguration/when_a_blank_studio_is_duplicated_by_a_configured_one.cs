// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Identity.Configuration;
using Cratis.Identity.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_HostedSharedConfiguration;

public class when_a_blank_studio_is_duplicated_by_a_configured_one : given.a_hosted_studio_configuration
{
    protected override string? StudioUrl => null;

    protected override string? StudioSecret => null;

    IdentityOptions _options;
    IReadOnlyList<string> _disabled;
    Exception _startup;

    void Because()
    {
        var configuration = new ConfigurationBuilder().AddConfiguration(_configuration).AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Identity:Resources:3:Audience"] = "studio-mcp",
            ["Identity:Resources:3:Indicators:0"] = "https://studio.example/mcp-operator",
            ["Identity:Resources:3:Scopes:0:Name"] = "studio:mcp:operator",
            ["Identity:Resources:3:TenantsCallback:Url"] = "https://studio.example/tenants",
            ["Identity:Resources:3:TenantsCallback:Secret"] = new string('y', 32)
        }).Build();
        _configuration = configuration;
        var environment = new HostingEnvironment { EnvironmentName = Environments.Production };
        _options = Bound();
        _disabled = new HostedSharedConfiguration(configuration, environment).DisabledAudiences(_options);

        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IChronicleClient>());
        _startup = Catch.Exception(() => services.AddCratisIdentity(configuration, environment));
    }

    [Fact] void should_have_both_entries_bound() => _options.Resources.Count(_ => _.Audience == "studio-mcp").ShouldEqual(2);
    [Fact] void should_not_disable_the_ambiguous_audience() => _disabled.ShouldBeEmpty();
    [Fact] void should_refuse_the_configuration() => Validate().Failed.ShouldBeTrue();
    [Fact] void should_refuse_startup() => _startup.ShouldBeOfExactType<OptionsValidationException>();
}

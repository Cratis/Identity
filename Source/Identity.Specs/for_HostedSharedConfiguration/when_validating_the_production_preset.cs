// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace Cratis.Identity.for_HostedSharedConfiguration;

public class when_validating_the_production_preset : Specification
{
    [Fact]
    void should_report_all_missing_settings_without_echoing_secrets()
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["Identity:GitHub:ClientSecret"] = "synthetic-sensitive-value",
            ["Identity:MongoDB:ConnectionString"] = "mongodb://user:synthetic-sensitive-value@localhost"
        });
        var options = new IdentityOptions();
        configuration.GetSection("Identity").Bind(options);
        var result = new HostedSharedConfiguration(configuration, new HostingEnvironment { EnvironmentName = Environments.Production }).Validate(null, options);
        result.Failed.ShouldBeTrue();
        var failures = string.Join('\n', result.Failures);
        failures.ShouldContain("Identity:Issuer");
        failures.ShouldContain("Identity:SecretStore:Path");
        failures.ShouldContain("Identity:GitHub:ClientId");
        failures.ShouldContain("Identity:MongoDB:Database");
        failures.ShouldContain("Identity:Revocation:FenceLog:InstanceId");
        failures.ShouldContain("Cratis:Chronicle:ConnectionString");
        failures.Contains("synthetic-sensitive-value", StringComparison.Ordinal).ShouldBeFalse();
        failures.Contains("mongodb://", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Fact]
    void should_not_change_development_validation() => new HostedSharedConfiguration(Configuration([]), new HostingEnvironment { EnvironmentName = Environments.Development }).Validate(null, new()).Skipped.ShouldBeTrue();

    [Fact]
    void should_not_change_the_reference_host() => new HostedSharedConfiguration(new ConfigurationBuilder().Build(), new HostingEnvironment { EnvironmentName = Environments.Production }).Validate(null, new()).Skipped.ShouldBeTrue();

    [Fact]
    void should_not_copy_the_hosted_preset_over_the_reference_configuration()
    {
        var configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json", optional: true).Build();
        (configuration["Identity:Hosting:Preset"] == "hosted-shared").ShouldBeFalse();
    }

    internal static IConfiguration Configuration(Dictionary<string, string?> settings)
    {
        settings["Identity:Hosting:Preset"] = "hosted-shared";
        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }
}

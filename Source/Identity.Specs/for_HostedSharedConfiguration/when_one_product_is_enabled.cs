// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Cratis.Identity.Configuration;
using Cratis.Identity.Hosting;
using Cratis.Identity.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_HostedSharedConfiguration;

public class when_one_product_is_enabled : Specification
{
    const string Audience = "example-mcp";
    IConfiguration _configuration;
    IdentityOptions _options;
    ValidateOptionsResult _result;
    ResourceCatalog _catalog;

    void Establish()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Identity:Issuer"] = "https://identity.example/",
            ["Identity:SecretStore:Path"] = "/run/identity/keys",
            ["Identity:GitHub:ClientId"] = "synthetic-client",
            ["Identity:GitHub:ClientSecret"] = "synthetic-upstream-secret",
            ["Identity:MongoDB:ConnectionString"] = "mongodb://operational.example",
            ["Identity:MongoDB:Database"] = "identity-example",
            ["Identity:Accounts:LookupKey"] = Convert.ToBase64String(new byte[32]),
            ["Identity:SecurityOwners:0"] = "synthetic-owner",
            ["Identity:FenceLog:ConnectionString"] = "mongodb://independent.example",
            ["Identity:FenceLog:Database"] = "fences",
            ["Identity:FenceLog:InstanceId"] = "synthetic-custody",
            ["Identity:FenceLog:MinimumSequence"] = "0",
            ["Cratis:Chronicle:ConnectionString"] = "chronicle://chronicle.example:35000",
            ["Identity:Hosting:EnabledAudiences:0"] = Audience,
            ["Identity:Resources:3:Audience"] = Audience,
            ["Identity:Resources:3:DisplayName"] = "Example MCP",
            ["Identity:Resources:3:ReceivesUpstreamIdentity"] = "true",
            ["Identity:Resources:3:Indicators:0"] = "https://app.example/mcp",
            ["Identity:Resources:3:Scopes:0:Name"] = "example:mcp:read",
            ["Identity:Resources:3:Scopes:0:Description"] = "Read the organization through MCP.",
            ["Identity:Resources:3:TenantsCallback:Url"] = "https://app.example/api/internal/assistants/tenants",
            ["Identity:Resources:3:TenantsCallback:Secret"] = new string('z', 32)
        };
        _configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("CratisId.appsettings.json")
            .AddInMemoryCollection(settings).Build();
    }

    void Because()
    {
        _options = new IdentityOptions();
        _configuration.GetSection("Identity").Bind(_options);
        var validator = new HostedSharedConfiguration(_configuration, new HostingEnvironment { EnvironmentName = Environments.Production });
        _result = validator.Validate(null, _options);
        HostedSharedConfiguration.Remove(_options, validator.DisabledAudiences(_options));
        _catalog = new ResourceCatalog(Options.Create(_options));
    }

    [Fact] void should_accept_the_configuration_without_the_other_products_callbacks() => _result.Succeeded.ShouldBeTrue();
    [Fact] void should_serve_only_the_named_product() => _catalog.All.Count().ShouldEqual(1);
    [Fact] void should_know_the_named_audience() => _catalog.FindByAudience(Audience).ShouldNotBeNull();
    [Fact] void should_not_know_the_direct_audiences() => (_catalog.FindByAudience("direct-mcp") ?? _catalog.FindByAudience("direct-v1")).ShouldBeNull();
    [Fact] void should_not_know_the_studio_audience() => _catalog.FindByAudience("studio-mcp").ShouldBeNull();
    [Fact] void should_not_own_the_direct_scopes() => _catalog.FindByScope("direct:mcp:read").ShouldBeEmpty();
}

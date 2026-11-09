// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Clients;
using Cratis.Identity.Configuration;
using Cratis.Identity.Hosting;
using Cratis.Identity.Protocol.Stores;
using Cratis.Identity.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_HostedSharedConfiguration;

public class when_loading_the_hosted_direct_resources : Specification
{
    IdentityOptions _options;
    ResourceCatalog _catalog;
    GrantPolicy _policy;
    IConfiguration _configuration;

    void Establish()
    {
        _configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("CratisId.appsettings.json")
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Identity:Issuer"] = "https://identity.example/",
                ["Identity:SecretStore:Path"] = "/run/identity/keys",
                ["Identity:GitHub:ClientId"] = "synthetic-client",
                ["Identity:GitHub:ClientSecret"] = "synthetic-upstream-secret",
                ["Identity:MongoDB:ConnectionString"] = "mongodb://operational.example",
                ["Identity:MongoDB:Database"] = "identity-products",
                ["Identity:Accounts:LookupKey"] = Convert.ToBase64String(new byte[32]),
                ["Identity:SecurityOwners:0"] = "synthetic-owner",
                ["Identity:FenceLog:ConnectionString"] = "mongodb://independent.example",
                ["Identity:FenceLog:Database"] = "fences",
                ["Identity:FenceLog:InstanceId"] = "synthetic-custody",
                ["Identity:FenceLog:MinimumSequence"] = "0",
                ["Cratis:Chronicle:ConnectionString"] = "chronicle://chronicle.example:35000",
                ["Identity:Resources:0:TenantsCallback:Url"] = "https://direct.example/tenants",
                ["Identity:Resources:0:TenantsCallback:Secret"] = new string('x', 32),
                ["Identity:Resources:1:TenantsCallback:Url"] = "https://direct.example/tenants",
                ["Identity:Resources:1:TenantsCallback:Secret"] = new string('x', 32),
                ["Identity:Resources:2:Indicators:0"] = "https://studio.example/mcp",
                ["Identity:Resources:2:TenantsCallback:Url"] = "https://studio.example/tenants",
                ["Identity:Resources:2:TenantsCallback:Secret"] = new string('x', 32)
            }).Build();
        _options = new IdentityOptions();
        _configuration.GetSection("Identity").Bind(_options);
        _catalog = new ResourceCatalog(Options.Create(_options));
        _policy = new GrantPolicy(_catalog);
    }

    [Fact] void should_accept_complete_production_settings() => new HostedSharedConfiguration(_configuration, new HostingEnvironment { EnvironmentName = Environments.Production }).Validate(null, _options).Succeeded.ShouldBeTrue();
    [Fact] void should_have_the_two_direct_surfaces_and_studio() => _catalog.All.Count().ShouldEqual(3);
    [Fact] void should_keep_the_api_audience() => _catalog.FindByIndicator("https://cratis.direct/v1").Audience.ShouldEqual("direct-v1");
    [Fact] void should_keep_the_mcp_audience() => _catalog.FindByIndicator("https://cratis.direct/mcp").Audience.ShouldEqual("direct-api");
    [Fact] void should_not_have_fixed_tenants() => _options.Resources.All(resource => resource.Tenants.Count == 0).ShouldBeTrue();
    [Fact] void should_not_mix_surface_scopes() => (_policy.Evaluate(new GrantRequest(["direct:read", "direct:mcp:read"], ["https://cratis.direct/v1", "https://cratis.direct/mcp"])) is NotGrantable).ShouldBeTrue();
    [Fact] void should_not_grant_api_scopes_to_mcp() => (_policy.Evaluate(new GrantRequest(["direct:read"], ["https://cratis.direct/mcp"])) is NotGrantable).ShouldBeTrue();
    [Fact] void should_keep_the_cli_valid() => ClientRegistrationRules.Validate(new ClientDescriptors(_catalog, Options.Create(_options)).CratisCliDescriptor(), [CodeChallengeMethods.Sha256]).IsValid.ShouldBeTrue();
    [Fact] void should_keep_the_hosted_validator_internal() => typeof(HostedSharedConfiguration).IsNotPublic.ShouldBeTrue();
    [Fact] void should_only_use_the_explicit_security_owner() => _options.SecurityOwners.SequenceEqual(["synthetic-owner"]).ShouldBeTrue();
    [Fact] void should_cap_access_tokens_at_five_minutes() => _options.Tokens.AccessTokenLifetime.ShouldEqual(TimeSpan.FromMinutes(5));

    [Fact]
    void should_keep_client_ids_stable_on_repeated_startup()
    {
        var first = new ConfigurationProtocolCatalog(new ClientDescriptors(_catalog, Options.Create(_options)), _catalog);
        var second = new ConfigurationProtocolCatalog(new ClientDescriptors(_catalog, Options.Create(_options)), _catalog);
        first.Applications.Select(application => application.Id).SequenceEqual(second.Applications.Select(application => application.Id)).ShouldBeTrue();
        first.Scopes.Count.ShouldEqual(8);
        second.Scopes.Count.ShouldEqual(8);
    }

    [Theory]
    [InlineData("http://direct.example/tenants", "long-enough")]
    [InlineData("https://direct.example/tenants", "short")]
    public void should_refuse_unsafe_callbacks(string url, string secret)
    {
        _options.Resources[0].TenantsCallback.Url = url;
        _options.Resources[0].TenantsCallback.Secret = secret == "short" ? "short" : new string('x', 32);
        new HostedSharedConfiguration(_configuration, new HostingEnvironment { EnvironmentName = Environments.Production }).Validate(null, _options).Failed.ShouldBeTrue();
    }

    [Fact]
    void should_refuse_configured_tenants()
    {
        _options.Resources[0].Tenants.Add(new TenantOptions { Id = "synthetic-tenant" });
        new HostedSharedConfiguration(_configuration, new HostingEnvironment { EnvironmentName = Environments.Production }).Validate(null, _options).Failed.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Identity:Issuer", "", "Identity:Issuer")]
    [InlineData("Identity:Issuer", "http://identity.example/", "Identity:Issuer")]
    [InlineData("Identity:SecretStore:Path", "", "Identity:SecretStore:Path")]
    [InlineData("Identity:MongoDB:ConnectionString", "", "Identity:MongoDB:ConnectionString")]
    [InlineData("Identity:MongoDB:Database", "", "Identity:MongoDB:Database")]
    [InlineData("Identity:FenceLog:ConnectionString", "", "Identity:FenceLog:ConnectionString")]
    [InlineData("Identity:FenceLog:ConnectionString", "mongodb://operational.example", "Identity:FenceLog:ConnectionString")]
    [InlineData("Identity:FenceLog:InstanceId", "", "Identity:FenceLog:InstanceId")]
    [InlineData("Identity:FenceLog:MinimumSequence", "", "Identity:FenceLog:MinimumSequence")]
    [InlineData("Identity:FenceLog:MinimumSequence", "-1", "Identity:FenceLog:MinimumSequence")]
    [InlineData("Identity:GitHub:ClientId", "", "Identity:GitHub:ClientId")]
    [InlineData("Identity:GitHub:ClientSecret", "", "Identity:GitHub:ClientSecret")]
    [InlineData("Identity:SecurityOwners:0", "", "Identity:SecurityOwners")]
    [InlineData("Identity:Tokens:AccessTokenLifetime", "00:05:01", "Identity:Tokens:AccessTokenLifetime")]
    [InlineData("Cratis:Chronicle:ConnectionString", "", "Cratis:Chronicle:ConnectionString")]
    public void should_refuse_an_incomplete_production_setting(string key, string value, string expectedKey)
    {
        _configuration[key] = value;
        var options = new IdentityOptions();
        _configuration.GetSection("Identity").Bind(options);
        var result = new HostedSharedConfiguration(_configuration, new HostingEnvironment { EnvironmentName = Environments.Production }).Validate(null, options);
        result.Failed.ShouldBeTrue();
        string.Join('\n', result.Failures).ShouldContain(expectedKey);
    }

    [Fact]
    void should_refuse_a_missing_callback()
    {
        _options.Resources[0].TenantsCallback = null;
        new HostedSharedConfiguration(_configuration, new HostingEnvironment { EnvironmentName = Environments.Production }).Validate(null, _options).Failed.ShouldBeTrue();
    }

    [Fact]
    void should_refuse_missing_account_lookup_material()
    {
        _options.Accounts.LookupKey = string.Empty;
        new HostedSharedConfiguration(_configuration, new HostingEnvironment { EnvironmentName = Environments.Production }).Validate(null, _options).Failed.ShouldBeTrue();
    }
}

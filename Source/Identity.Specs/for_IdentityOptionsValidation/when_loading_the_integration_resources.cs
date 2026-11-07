// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Integration.given;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_loading_the_integration_resources : Specification
{
    IdentityOptions _options;

    void Because()
    {
        // Match IdentityHost: keep the shipped JSON, then overlay the same resource settings in memory.
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("Identity.appsettings.json")
            .AddInMemoryCollection(ResourceSettings.Create("cratis", "second", "134365"))
            .Build();
        _options = new IdentityOptions();
        configuration.GetSection(IdentityOptions.SectionName).Bind(_options);
        (configuration as IDisposable)?.Dispose();
    }

    [Fact] void should_validate_the_merged_configuration() => new IdentityOptionsValidation(new HostingEnvironment { EnvironmentName = Environments.Development }).Validate(null, _options).Succeeded.ShouldBeTrue();
    [Fact] void should_preserve_both_direct_resources_and_append_the_test_resources() => _options.Resources.Select(resource => resource.Audience).SequenceEqual(["direct-api", "direct-mcp", "studio-api", "stage-api"]).ShouldBeTrue();
    [Fact] void should_keep_the_api_indicator() => Resource("direct-api").Indicators.Single().ShouldEqual("https://cratis.direct/v1");
    [Fact] void should_keep_the_mcp_indicator() => Resource("direct-mcp").Indicators.Single().ShouldEqual("https://cratis.direct/mcp");
    [Fact] void should_keep_the_api_scopes() => Resource("direct-api").Scopes.Select(scope => scope.Name).SequenceEqual(["direct:read", "direct:content.write", "direct:work"]).ShouldBeTrue();
    [Fact] void should_keep_the_mcp_scopes() => Resource("direct-mcp").Scopes.Select(scope => scope.Name).SequenceEqual(["direct:mcp:read", "direct:mcp:content.write", "direct:mcp:work"]).ShouldBeTrue();
    [Fact] void should_configure_both_api_tenants() => Resource("direct-api").Tenants.Select(tenant => tenant.Id).SequenceEqual(["cratis", "second"]).ShouldBeTrue();
    [Fact] void should_configure_both_mcp_tenants() => Resource("direct-mcp").Tenants.Select(tenant => tenant.Id).SequenceEqual(["cratis", "second"]).ShouldBeTrue();
    [Fact] void should_keep_the_member_in_both_direct_resources() => _options.Resources.Take(2).SelectMany(resource => resource.Tenants).All(tenant => tenant.Members.SequenceEqual(["134365"])).ShouldBeTrue();
    [Fact] void should_register_studio_without_inherited_direct_scopes() => Resource("studio-api").Scopes.Single().Name.ShouldEqual("studio:read");
    [Fact] void should_keep_the_studio_indicator() => Resource("studio-api").Indicators.Single().ShouldEqual("https://cratis.studio/mcp");
    [Fact] void should_keep_the_open_studio_tenant() => Resource("studio-api").Tenants.Single().AllowAllAccounts.ShouldBeTrue();
    [Fact] void should_register_the_stage_scope() => Resource("stage-api").Scopes.Single().Name.ShouldEqual("stage:read");
    [Fact] void should_keep_the_stage_indicator() => Resource("stage-api").Indicators.Single().ShouldEqual("https://stage.example/api");
    [Fact] void should_keep_the_stage_callback() => Resource("stage-api").TenantsCallback.Url.ShouldEqual("https://products.example/tenants");
    [Fact] void should_not_add_configured_tenants_to_stage() => Resource("stage-api").Tenants.ShouldBeEmpty();

    ResourceServerOptions Resource(string audience) => _options.Resources.Single(resource => resource.Audience == audience);
}

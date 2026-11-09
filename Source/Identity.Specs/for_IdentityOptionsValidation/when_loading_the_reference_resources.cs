// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Clients;
using Cratis.Identity.Configuration;
using Cratis.Identity.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_loading_the_reference_resources : Specification
{
    IdentityOptions _options;
    ResourceCatalog _catalog;
    GrantPolicy _policy;

    void Establish()
    {
        using var configuration = (ConfigurationRoot)new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("Identity.appsettings.json").Build();
        _options = new IdentityOptions();
        configuration.GetSection(IdentityOptions.SectionName).Bind(_options);
        _catalog = new ResourceCatalog(Options.Create(_options));
        _policy = new GrantPolicy(_catalog);
    }

    [Fact] void should_validate_the_configuration() => new IdentityOptionsValidation(new HostingEnvironment { EnvironmentName = Environments.Development }).Validate(null, _options).Succeeded.ShouldBeTrue();
    [Fact] void should_use_a_separate_v1_audience() => _catalog.FindByIndicator("https://cratis.direct/v1").Audience.ShouldEqual("direct-v1");
    [Fact] void should_keep_the_n1_mcp_audience() => _catalog.FindByIndicator("https://cratis.direct/mcp").Audience.ShouldEqual("direct-api");
    [Fact] void should_have_one_indicator_per_audience() => _catalog.All.All(resource => resource.Indicators.Count() == 1).ShouldBeTrue();
    [Fact] void should_not_share_scopes() => _catalog.All.SelectMany(resource => resource.Scopes).GroupBy(scope => scope.Name).All(group => group.Count() == 1).ShouldBeTrue();
    [Fact] void should_refuse_a_multi_resource_grant() => (_policy.Evaluate(new GrantRequest(["direct:read", "direct:mcp:read"], ["https://cratis.direct/v1", "https://cratis.direct/mcp"])) is NotGrantable).ShouldBeTrue();
    [Fact] void should_not_grant_api_scopes_to_mcp() => (_policy.Evaluate(new GrantRequest(["direct:read"], ["https://cratis.direct/mcp"])) is NotGrantable).ShouldBeTrue();
    [Fact] void should_keep_the_cli_valid() => ClientRegistrationRules.Validate(new ClientDescriptors(_catalog, Options.Create(_options)).CratisCliDescriptor(), [CodeChallengeMethods.Sha256]).IsValid.ShouldBeTrue();
}

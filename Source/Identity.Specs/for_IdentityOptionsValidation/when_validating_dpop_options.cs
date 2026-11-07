// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.Extensions.Hosting.Internal;

namespace Cratis.Identity.for_IdentityOptionsValidation;

public class when_validating_dpop_options : Specification
{
    [Theory]
    [InlineData("", false)]
    [InlineData("/connect/token", false)]
    [InlineData("http://identity.example/connect/token", false)]
    [InlineData("https://identity.example/connect/token?x=1", false)]
    [InlineData("https://identity.example/connect/token#x", false)]
    [InlineData("https://user@identity.example/connect/token", false)]
    [InlineData("https://identity.example/connect/token", true)]
    [InlineData("http://127.0.0.1/connect/token", true)]
    void should_validate_the_configured_public_endpoint(string endpoint, bool accepted)
    {
        var options = Configurations.TwoResourceServers();
        options.DPoP.Enabled = true;
        options.DPoP.PublicTokenEndpoint = endpoint;
        new IdentityOptionsValidation(new HostingEnvironment { EnvironmentName = "Development" }).Validate(null, options).Succeeded.ShouldEqual(accepted);
    }

    [Fact]
    void should_reject_unknown_required_clients()
    {
        var options = Configurations.TwoResourceServers();
        options.DPoP.Enabled = true;
        options.DPoP.PublicTokenEndpoint = "https://identity.example/connect/token";
        options.DPoP.RequiredClients.Add("unknown");
        new IdentityOptionsValidation(new HostingEnvironment { EnvironmentName = "Development" }).Validate(null, options).Succeeded.ShouldBeFalse();
    }

    [Fact]
    void should_not_validate_unused_disabled_configuration()
    {
        var options = Configurations.TwoResourceServers();
        options.DPoP.RequiredClients.Add("unknown");
        new IdentityOptionsValidation(new HostingEnvironment { EnvironmentName = "Development" }).Validate(null, options).Succeeded.ShouldBeTrue();
    }

    [Fact] void should_be_disabled_by_default() => new IdentityOptions().DPoP.Enabled.ShouldBeFalse();
}

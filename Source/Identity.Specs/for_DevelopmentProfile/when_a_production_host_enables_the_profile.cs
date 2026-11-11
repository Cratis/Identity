// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_a_production_host_enables_the_profile : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => new ServiceCollection().AddCratisIdentity(
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Identity:Issuer"] = "http://localhost:5100/",
            ["Identity:Development:Enabled"] = "true"
        }).Build(),
        new HostingEnvironment { EnvironmentName = Environments.Production }));

    [Fact] void should_refuse_to_start_with_the_development_message() => _exception.ShouldBeOfExactType<DevelopmentProfileOnlyInDevelopment>();
}

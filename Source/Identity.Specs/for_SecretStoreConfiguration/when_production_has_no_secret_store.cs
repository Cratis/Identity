// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace Cratis.Identity.for_SecretStoreConfiguration;

public class when_production_has_no_secret_store : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => new ServiceCollection().AddCratisIdentity(
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Identity:Issuer"] = "https://auth.example/" }).Build(),
        new HostingEnvironment { EnvironmentName = Environments.Production }));

    [Fact] void should_refuse_to_start() => _exception.ShouldBeOfExactType<SecretStoreNotConfigured>();
}

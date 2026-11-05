// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace Cratis.Identity.for_SecretStoreConfiguration;

public class when_keys_are_configured_in_settings : Specification
{
    List<Exception?> _exceptions;

    void Because() => _exceptions = new[] { "Identity:SigningCertificate:Path", "Identity:EncryptionCertificate:Password" }
        .Select(setting => Catch.Exception(() => new ServiceCollection().AddCratisIdentity(
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { [setting] = "x" }).Build(),
            new HostingEnvironment { EnvironmentName = Environments.Development })))
        .ToList();

    [Fact] void should_refuse_each_setting_even_in_development() => _exceptions.TrueForAll(_ => _ is KeysInConfiguration).ShouldBeTrue();
    [Fact] void should_name_the_secret_store_layout() => _exceptions.TrueForAll(_ => _!.Message.Contains("Identity:SecretStore:Path", StringComparison.Ordinal)).ShouldBeTrue();
}

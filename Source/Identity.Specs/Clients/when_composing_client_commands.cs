// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Clients;

public class when_composing_client_commands : given.client_commands
{
    [Fact]
    void should_register_the_producer_from_options()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Identity:ClientSecrets:Key"] = Convert.ToBase64String(Enumerable.Range(0, 32).Select(value => (byte)value).ToArray()),
            ["Identity:ClientSecrets:Revision"] = "2"
        }).Build();
        var services = new ServiceCollection();
        services.AddClients(configuration);
        using var provider = services.BuildServiceProvider();
        var digest = provider.GetRequiredService<ClientSecretDigester>().Digest(Plaintext);
        SecretDigest.IsWellFormed(digest.Value).ShouldBeTrue();
        digest.Value.StartsWith("v2.", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    void should_fail_closed_without_a_configured_key()
    {
        var services = new ServiceCollection();
        services.AddClients(new ConfigurationBuilder().Build());
        using var provider = services.BuildServiceProvider();
        var error = Catch.Exception(() => _ = provider.GetRequiredService<ClientSecretDigester>());
        error.ShouldBeOfExactType<InvalidOperationException>();
        error.Message.ShouldEqual("invalid_client_secret_key_configuration");
    }
}

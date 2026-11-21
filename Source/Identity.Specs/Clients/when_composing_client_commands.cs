// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.Registering;
using Cratis.Identity.Clients.RotatingSecret;
using Cratis.Identity.Contracts.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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

    [Theory]
    [InlineData(" ")]
    [InlineData("not-base64")]
    [InlineData("AA==")]
    async Task should_fail_on_host_start_before_the_producer_is_resolved(string key)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Identity:ClientSecrets:Key"] = key
        }).Build();
        using var host = new HostBuilder().ConfigureServices(services => services.AddClients(configuration)).Build();
        var error = await Record.ExceptionAsync(() => host.StartAsync(TestContext.Current.CancellationToken));
        error.ShouldBeOfExactType<OptionsValidationException>();
        ((OptionsValidationException)error).Failures.ShouldContain("invalid_client_secret_key_configuration");
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData("", 1)]
    [InlineData(null, 0)]
    [InlineData(null, -1)]
    async Task should_start_without_a_key_but_refuse_secret_producing_commands(string? key, int revision)
    {
        var values = new Dictionary<string, string?>();
        if (key is not null)
        {
            values["Identity:ClientSecrets:Key"] = key;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        using var host = new HostBuilder().ConfigureServices(services =>
        {
            services.AddClients(configuration);
            services.Configure<ClientSecretOptions>(options => options.Revision = revision);
        }).Build();
        await host.StartAsync(TestContext.Current.CancellationToken);
        var registration = new RegisterClient(new("keyless-spec-client"), _configuration, Plaintext);
        var registrationError = Catch.Exception(() => _ = registration.Handle(_descriptors, _clock, host.Services.GetRequiredService<ClientSecretDigester>()));
        registrationError.ShouldBeOfExactType<InvalidOperationException>();
        registrationError.Message.ShouldEqual("invalid_client_secret_key_configuration");
        var rotation = new RotateSecret(_id, Plaintext, new(Now, Now));
        var rotationError = Catch.Exception(() => _ = rotation.Handle(_active, host.Services.GetRequiredService<ClientSecretDigester>(), _clock, host.Services.GetRequiredService<IOptions<ClientSecretOptions>>()));
        rotationError.ShouldBeOfExactType<InvalidOperationException>();
        rotationError.Message.ShouldEqual("invalid_client_secret_key_configuration");
        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    async Task should_start_with_a_valid_server_key()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Identity:ClientSecrets:Key"] = Convert.ToBase64String(Enumerable.Range(0, 32).Select(value => (byte)value).ToArray())
        }).Build();
        using var host = new HostBuilder().ConfigureServices(services => services.AddClients(configuration)).Build();
        await host.StartAsync(TestContext.Current.CancellationToken);
        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(0, 7)]
    [InlineData(-1, 7)]
    [InlineData(1, -1)]
    async Task should_reject_invalid_revision_or_overlap_options_on_start(int revision, int overlapDays)
    {
        using var host = new HostBuilder().ConfigureServices(services =>
        {
            services.AddClients(new ConfigurationBuilder().Build());
            services.Configure<ClientSecretOptions>(options =>
            {
                options.Key = _secretOptions.Value.Key;
                options.Revision = revision;
                options.MaxOverlap = TimeSpan.FromDays(overlapDays);
            });
        }).Build();
        var error = await Record.ExceptionAsync(() => host.StartAsync(TestContext.Current.CancellationToken));
        error.ShouldBeOfExactType<OptionsValidationException>();
        ((OptionsValidationException)error).Failures.ShouldContain("invalid_client_secret_policy_configuration");
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

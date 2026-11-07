// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Cratis.Identity.Integration.given;

/// <summary>
/// A pinned development kernel with bundled storage, isolated from production and bound only to loopback.
/// </summary>
public sealed class ChronicleKernel : IAsyncDisposable
{
    const int ClientPort = 35000;
    readonly IContainer _container = new ContainerBuilder(Environment.GetEnvironmentVariable("IDENTITY_CHRONICLE_IMAGE") ?? "cratis/chronicle:19.111.0-development")
        .WithPortBinding(ClientPort, true)
        .WithCreateParameterModifier(parameters =>
        {
            foreach (var binding in parameters.HostConfig.PortBindings[$"{ClientPort}/tcp"])
            {
                binding.HostIP = "127.0.0.1";
            }
        })
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(ClientPort, wait => wait.WithTimeout(TimeSpan.FromMinutes(2))))
        .Build();

    /// <summary>Gets the local test endpoint. Development authentication is provided by the native client's development defaults.</summary>
    public string ConnectionString => $"chronicle://{_container.Hostname}:{_container.GetMappedPublicPort(ClientPort)}?skipTlsValidation=true";

    /// <summary>Starts the shared kernel once for its owning host fixture.</summary>
    /// <returns>The startup task.</returns>
    public Task Start() => _container.StartAsync();

    /// <summary>Stops the real kernel for outage scenarios; its state is retained for restart.</summary>
    /// <returns>The stopped container.</returns>
    public Task Stop() => _container.StopAsync();

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => _container.DisposeAsync();
}

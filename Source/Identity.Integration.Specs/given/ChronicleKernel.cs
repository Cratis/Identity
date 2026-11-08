// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Sockets;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Cratis.Identity.Integration.given;

/// <summary>
/// A pinned development kernel with bundled storage, isolated from production and bound only to loopback.
/// </summary>
public sealed class ChronicleKernel : IAsyncDisposable
{
    const int ClientPort = 35000;
    readonly IContainer _container;

    /// <summary>Initializes a new instance of the <see cref="ChronicleKernel"/> class.</summary>
    public ChronicleKernel()
    {
        // A fixed host port keeps the endpoint stable across the outage specs' stop/start; a Docker-assigned port changes on restart.
        var hostPort = FreeLoopbackPort();
        _container = new ContainerBuilder(Environment.GetEnvironmentVariable("IDENTITY_CHRONICLE_IMAGE") ?? "cratis/chronicle:19.111.0-development")
            .WithPortBinding(hostPort, ClientPort)
            .WithCreateParameterModifier(parameters =>
            {
                foreach (var binding in parameters.HostConfig.PortBindings[$"{ClientPort}/tcp"])
                {
                    binding.HostIP = "127.0.0.1";
                }
            })
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(ClientPort, wait => wait.WithTimeout(TimeSpan.FromMinutes(2))))
            .Build();
    }

    /// <summary>Gets the local test endpoint. Development authentication is provided by the native client's development defaults.</summary>
    public string ConnectionString => $"chronicle://{_container.Hostname}:{_container.GetMappedPublicPort(ClientPort)}?skipTlsValidation=true";

    /// <summary>Starts the shared kernel once for its owning host fixture.</summary>
    /// <returns>The startup task.</returns>
    /// <exception cref="TimeoutException">The kernel did not become ready in time.</exception>
    public async Task Start()
    {
        await _container.StartAsync();

        // The client port accepts TCP before the kernel is ready; its token endpoint answers 503 until then.
#pragma warning disable MA0039 // Loopback development kernel with a self-signed certificate.
        using var handler = new HttpClientHandler { CheckCertificateRevocationList = true, ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator };
#pragma warning restore MA0039
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        var uri = new Uri($"https://{_container.Hostname}:{_container.GetMappedPublicPort(ClientPort)}/connect/token");
        var deadline = DateTime.UtcNow.AddMinutes(4);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var response = await http.PostAsync(uri, new FormUrlEncodedContent([]));
                if (response.StatusCode != HttpStatusCode.ServiceUnavailable)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException)
            {
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        throw new TimeoutException("The Chronicle kernel did not become ready.");
    }

    /// <summary>Stops the real kernel for outage scenarios; its state is retained for restart.</summary>
    /// <returns>The stopped container.</returns>
    public Task Stop() => _container.StopAsync();

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => _container.DisposeAsync();

    static int FreeLoopbackPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}

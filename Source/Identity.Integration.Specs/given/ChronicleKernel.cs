// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Net.Sockets;
using Docker.DotNet;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Cratis.Identity.Integration.given;

/// <summary>
/// A pinned development kernel with bundled storage, isolated from production and bound only to loopback.
/// </summary>
public sealed class ChronicleKernel : IAsyncDisposable
{
    const int ClientPort = 35000;
    const int OutputTailLines = 60;
    readonly IContainer _container;

    /// <summary>Initializes a new instance of the <see cref="ChronicleKernel"/> class.</summary>
    public ChronicleKernel()
    {
        // A fixed host port keeps the endpoint stable across the outage specs' stop/start; a Docker-assigned port changes on restart.
        var hostPort = FreeLoopbackPort();
        _container = new ContainerBuilder(Environment.GetEnvironmentVariable("IDENTITY_CHRONICLE_IMAGE") ?? "cratis/chronicle:19.181.5-development")
            .WithPortBinding(hostPort, ClientPort)
            .WithCreateParameterModifier(parameters =>
            {
                // Reserve the kernel listener port from outbound ephemeral allocation before Kestrel binds.
                parameters.HostConfig.Sysctls ??= new Dictionary<string, string>(StringComparer.Ordinal);
                parameters.HostConfig.Sysctls["net.ipv4.ip_local_reserved_ports"] = ClientPort.ToString(CultureInfo.InvariantCulture);
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
    /// <exception cref="ChronicleKernelExited">The kernel container stopped before it became ready.</exception>
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
        var lastOutcome = "no response";
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var response = await http.PostAsync(uri, new FormUrlEncodedContent([]));
                if (response.StatusCode != HttpStatusCode.ServiceUnavailable)
                {
                    return;
                }

                lastOutcome = "503 Service Unavailable";
            }
            catch (HttpRequestException ex)
            {
                // A refused connection can mean the kernel process is gone; waiting out the deadline then only hides why.
                lastOutcome = $"{ex.GetType().Name}: {ex.Message}";
                if (await ExitCodeIfStopped() is { } exitCode)
                {
                    throw new ChronicleKernelExited(exitCode, $"Last outcome: {lastOutcome}.\n{await OutputTail()}");
                }
            }
            catch (TaskCanceledException)
            {
                lastOutcome = "no answer within 5 seconds";
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        throw new TimeoutException($"The Chronicle kernel did not become ready within 4 minutes. Last outcome: {lastOutcome}.\n{await OutputTail()}");
    }

    /// <summary>Gets the end of the kernel's output, without letting a Docker failure replace the caller's own failure.</summary>
    /// <returns>The output tail, or why it is unavailable.</returns>
    public Task<string> Output() => OutputTail();

    /// <summary>Stops the real kernel for outage scenarios; its state is retained for restart.</summary>
    /// <returns>The stopped container.</returns>
    public Task Stop() => _container.StopAsync();

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => _container.DisposeAsync();

    static string Tail(string text) => string.Join('\n', text.Split('\n', StringSplitOptions.RemoveEmptyEntries).TakeLast(OutputTailLines));

    static int FreeLoopbackPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    static bool IsDockerFailure(Exception exception) =>
        exception is DockerApiException or HttpRequestException or IOException or OperationCanceledException;

    /// <summary>
    /// Gets the exit code when the container has stopped. Waiting on a running container blocks,
    /// so a short bound tells running from stopped; an unreachable Docker daemon counts as unknown.
    /// </summary>
    /// <returns>The exit code, or <see langword="null"/> while running or unknown.</returns>
    async Task<long?> ExitCodeIfStopped()
    {
        using var probe = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        try
        {
            return await _container.GetExitCodeAsync(probe.Token);
        }
        catch (Exception ex) when (IsDockerFailure(ex))
        {
            return null;
        }
    }

    /// <summary>Gets the end of the kernel's output, without letting a Docker failure replace the readiness failure.</summary>
    /// <returns>The output tail, or why it is unavailable.</returns>
    async Task<string> OutputTail()
    {
        using var bound = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            var (stdout, stderr) = await _container.GetLogsAsync(timestampsEnabled: true, ct: bound.Token);
            return $"Kernel output (last {OutputTailLines} lines):\n{Tail(stdout)}\nKernel errors (last {OutputTailLines} lines):\n{Tail(stderr)}";
        }
        catch (Exception ex) when (IsDockerFailure(ex))
        {
            return $"Kernel output unavailable: {ex.GetType().Name}: {ex.Message}";
        }
    }
}

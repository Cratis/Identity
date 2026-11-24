// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Protocol.Stores;

namespace Cratis.Identity.Clients.Serving;

/// <summary>
/// Serves the clients registered through the registration commands, kept as a snapshot that is replaced whole, so a request
/// never sees a half-updated list.
/// </summary>
/// <param name="services">Resolves what reads the client registrations when a refresh runs, so the protocol endpoints can be composed without a Chronicle connection.</param>
/// <param name="descriptors">The clients the configuration declares, which a registered client may not reuse the id of.</param>
/// <param name="logger">The logger.</param>
internal sealed partial class RegisteredClients(IServiceProvider services, ClientDescriptors descriptors, ILogger<RegisteredClients> logger) : IRegisteredClients
{
    volatile IReadOnlyList<ClientApplication> _applications = [];

    /// <inheritdoc/>
    public IReadOnlyList<ClientApplication> Applications => _applications;

    /// <inheritdoc/>
    public async Task Refresh(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var scope = services.CreateAsyncScope();
        var readModels = scope.ServiceProvider.GetRequiredService<IReadModels>();
        var registrations = await readModels.GetInstances<ClientRegistration>();
        _applications = Serve(registrations, descriptors.All().Select(descriptor => descriptor.ClientId).ToHashSet(StringComparer.Ordinal), logger);
    }

    /// <summary>Chooses which registrations are served.</summary>
    /// <param name="registrations">Every registration.</param>
    /// <param name="reserved">The client ids the configuration already declares.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The retired ones left out, and any that cannot be served left out with a log line.</returns>
    internal static IReadOnlyList<ClientApplication> Serve(IEnumerable<ClientRegistration> registrations, ISet<string?> reserved, ILogger logger)
    {
        var served = new List<ClientApplication>();
        foreach (var registration in registrations.Where(registration => registration is { Retired: false, ClientId: not null }))
        {
            if (reserved.Contains(registration.ClientId!.Value))
            {
                ClientIdReserved(logger, registration.Id);
                continue;
            }

            try
            {
                served.Add(new(RegisteredClientDescriptors.For(registration), registration));
            }
            catch (InvalidOperationException exception)
            {
                NotServed(logger, registration.Id, exception);
            }
        }

        return served.AsReadOnly();
    }

    [LoggerMessage(LogLevel.Error, "Registered client {Registration} is not served: it cannot be described as a protocol client")]
    static partial void NotServed(ILogger logger, Guid registration, Exception exception);

    [LoggerMessage(LogLevel.Error, "Registered client {Registration} is not served: its client id is declared by the configuration")]
    static partial void ClientIdReserved(ILogger logger, Guid registration);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Cratis.Identity.Clients.Serving;

namespace Cratis.Identity.Protocol.Stores;

/// <summary>
/// The protocol catalog the endpoints use: the clients and scopes the configuration declares, plus the clients registered
/// while Cratis Identity runs.
/// </summary>
/// <param name="configured">The catalog built from configuration.</param>
/// <param name="registered">The clients registered through the registration commands.</param>
internal sealed class ServedProtocolCatalog(IProtocolCatalog configured, IRegisteredClients registered) : IProtocolCatalog
{
    /// <inheritdoc/>
    public IReadOnlyList<ClientApplication> Applications => [.. configured.Applications, .. registered.Applications];

    /// <inheritdoc/>
    public IReadOnlyList<ResourceScope> Scopes => configured.Scopes;
}

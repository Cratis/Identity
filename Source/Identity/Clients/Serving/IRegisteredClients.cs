// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Cratis.Identity.Protocol.Stores;

namespace Cratis.Identity.Clients.Serving;

/// <summary>The clients registered while Cratis Identity runs, as the protocol endpoints serve them.</summary>
internal interface IRegisteredClients
{
    /// <summary>Gets the active registered clients as of the last refresh.</summary>
    IReadOnlyList<ClientApplication> Applications { get; }

    /// <summary>Reads the registrations again and replaces what is served.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    Task Refresh(CancellationToken cancellationToken = default);
}

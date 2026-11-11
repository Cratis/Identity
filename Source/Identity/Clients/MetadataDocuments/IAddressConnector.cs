// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Identity.Clients.MetadataDocuments;

/// <summary>Connects directly to a validated IP, with no second name resolution.</summary>
internal interface IAddressConnector
{
    /// <summary>Opens an owned stream to a validated address.</summary>
    /// <param name="address">One of the addresses already checked by policy.</param>
    /// <param name="port">The HTTPS port.</param>
    /// <param name="cancellationToken">The fetch deadline.</param>
    /// <returns>An owned network stream.</returns>
    ValueTask<Stream> ConnectAsync(IPAddress address, int port, CancellationToken cancellationToken);
}

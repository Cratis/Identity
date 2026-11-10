// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Identity.Clients.MetadataDocuments;

/// <summary>Resolves one complete DNS answer set for a connection attempt.</summary>
internal interface IHostResolver
{
    /// <summary>Resolves the host once.</summary>
    /// <param name="host">The original DNS host.</param>
    /// <param name="cancellationToken">The total fetch deadline.</param>
    /// <returns>All returned addresses.</returns>
    ValueTask<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken);
}

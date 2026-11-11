// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Identity.Clients.MetadataDocuments;

/// <summary>The production DNS resolver; used only inside the guarded connection callback.</summary>
internal sealed class HostResolver : IHostResolver
{
    /// <inheritdoc/>
    public async ValueTask<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken) => await Dns.GetHostAddressesAsync(host, cancellationToken);
}

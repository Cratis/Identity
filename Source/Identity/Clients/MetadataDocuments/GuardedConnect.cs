// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Identity.Configuration;

namespace Cratis.Identity.Clients.MetadataDocuments;

/// <summary>Checks every DNS answer and pins the connection to that checked answer set.</summary>
/// <param name="resolver">The one-shot DNS seam.</param>
/// <param name="connector">The direct-IP socket seam.</param>
/// <param name="options">Optional issuer-owned connection limits.</param>
/// <param name="time">The clock for connection deadlines.</param>
internal sealed class GuardedConnect(IHostResolver resolver, IAddressConnector connector, ClientIdMetadataOptions? options = null, TimeProvider? time = null)
{
    /// <summary>Creates a transport with no redirects, credentials, cookies, proxies or decompression.</summary>
    /// <param name="timeout">The connection deadline, within the total fetch deadline.</param>
    /// <returns>The guarded handler. TLS still uses the original host and default certificate validation.</returns>
    public SocketsHttpHandler CreateHandler(TimeSpan timeout) => new()
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        UseProxy = false,
        Credentials = null,
        PreAuthenticate = false,
        AutomaticDecompression = DecompressionMethods.None,
        MaxResponseHeadersLength = 8,
        ConnectTimeout = timeout,
        PooledConnectionLifetime = TimeSpan.FromMinutes(1),
        PooledConnectionIdleTimeout = TimeSpan.FromSeconds(30),
        ConnectCallback = (context, cancellation) => ConnectAsync(context.DnsEndPoint.Host, context.DnsEndPoint.Port, cancellation)
    };

    /// <summary>Resolves once, refuses the whole mixed/private set, and connects only a checked IP.</summary>
    /// <param name="host">The original HTTPS hostname.</param>
    /// <param name="port">The HTTPS port.</param>
    /// <param name="cancellationToken">The fetch deadline.</param>
    /// <returns>The pinned stream, with platform-default TLS and certificate validation applied by SocketsHttpHandler.</returns>
    /// <exception cref="HttpRequestException">The port or any DNS answer is refused.</exception>
    public async ValueTask<Stream> ConnectAsync(string host, int port, CancellationToken cancellationToken)
    {
        if (port != 443) throw new HttpRequestException("Metadata connection refused.");
        using var timeout = new CancellationTokenSource(options?.ConnectTimeout ?? TimeSpan.FromSeconds(3), time ?? TimeProvider.System);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        var addresses = await resolver.ResolveAsync(host, deadline.Token);
        if (addresses.Length == 0 || addresses.Any(_ => !AddressPolicy.IsPublic(_))) throw new HttpRequestException("Metadata address refused.");
        return await connector.ConnectAsync(addresses[0], port, deadline.Token);
    }
}

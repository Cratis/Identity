// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Integration.for_ClientIdMetadataDocuments.given;

/// <summary>Real Identity pipeline and stores; only the named outbound metadata HTTP handler is replaced.</summary>
public class MetadataIdentityHost : IdentityHost
{
    readonly ConcurrentDictionary<string, string> _documents = new(StringComparer.Ordinal);
    readonly ConcurrentDictionary<string, string> _cacheControls = new(StringComparer.Ordinal);
    readonly ConcurrentDictionary<string, int> _requests = new(StringComparer.Ordinal);

    /// <summary>Sets an exact synthetic URL response without using public DNS.</summary>
    /// <param name="clientId">The exact fetch URL.</param>
    /// <param name="body">The synthetic JSON body.</param>
    /// <param name="cacheControl">The synthetic response's cache policy.</param>
    public void SetDocument(string clientId, string body, string cacheControl = "max-age=60")
    {
        _cacheControls[clientId] = cacheControl;
        _documents[clientId] = body;
    }

    /// <summary>Counts actual outbound HTTP requests for an exact synthetic URL.</summary>
    /// <param name="clientId">The exact fetch URL.</param>
    /// <returns>The number of fetches.</returns>
    public int RequestsFor(string clientId) => _requests.GetValueOrDefault(clientId);

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Identity:Clients:MetadataDocuments:Enabled", "true").ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Identity:Clients:MetadataDocuments:Enabled"] = "true"
        }));
        builder.ConfigureTestServices(services => services.AddHttpClient("Identity.Cimd").ConfigurePrimaryHttpMessageHandler(() => new MetadataHandler(_documents, _cacheControls, _requests)));
    }

    sealed class MetadataHandler(ConcurrentDictionary<string, string> documents, ConcurrentDictionary<string, string> cacheControls, ConcurrentDictionary<string, int> requests) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var clientId = request.RequestUri.OriginalString;
            requests.AddOrUpdate(clientId, 1, (_, count) => count + 1);
            var found = documents.TryGetValue(clientId, out var body);
            var response = new HttpResponseMessage(found ? HttpStatusCode.OK : HttpStatusCode.NotFound)
            {
                Content = new StringContent(body ?? string.Empty, System.Text.Encoding.UTF8, "application/json")
            };
            response.Headers.CacheControl = CacheControlHeaderValue.Parse(cacheControls.GetValueOrDefault(clientId, "no-store"));
            return Task.FromResult(response);
        }
    }
}

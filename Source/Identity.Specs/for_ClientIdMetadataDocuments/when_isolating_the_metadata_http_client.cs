// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Identity.Clients.MetadataDocuments;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.for_ClientIdMetadataDocuments;

public class when_isolating_the_metadata_http_client : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_exclude_host_wide_retry_and_hedging_handlers(bool defaultsAddedLast)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (!defaultsAddedLast) services.ConfigureHttpClientDefaults(builder => builder.AddHttpMessageHandler(() => new ForbiddenHandler()));
        services.AddClientIdMetadataDocuments(enabled: true);
        if (defaultsAddedLast) services.ConfigureHttpClientDefaults(builder => builder.AddHttpMessageHandler(() => new ForbiddenHandler()));
        using var transport = new SingleResponseHandler();
        services.AddHttpClient("Identity.Cimd").ConfigurePrimaryHttpMessageHandler(() => transport);
        await using var provider = services.BuildServiceProvider();
        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("Identity.Cimd");
        using var response = await client.GetAsync(new Uri("https://client.example/client.json"), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldEqual(HttpStatusCode.OK);
        transport.Requests.ShouldEqual(1);
        client.Timeout.ShouldEqual(Timeout.InfiniteTimeSpan);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_not_send_host_wide_default_request_headers(bool defaultsAddedLast)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        static void addDefaults(IServiceCollection collection) => collection.ConfigureHttpClientDefaults(builder => builder.ConfigureHttpClient(client =>
        {
            client.DefaultRequestHeaders.Add("Authorization", "Bearer host-secret");
            client.DefaultRequestHeaders.Add("X-Api-Key", "host-key");
        }));

        if (!defaultsAddedLast) addDefaults(services);
        services.AddClientIdMetadataDocuments(enabled: true);
        if (defaultsAddedLast) addDefaults(services);
        var transport = new HeaderCapturingHandler();
        services.AddHttpClient("Identity.Cimd").ConfigurePrimaryHttpMessageHandler(() => transport);
        await using var provider = services.BuildServiceProvider();
        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("Identity.Cimd");
        using var response = await client.GetAsync(new Uri("https://client.example/client.json"), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldEqual(HttpStatusCode.OK);
        transport.Authorization.ShouldBeNull();
        transport.ApiKeyPresent.ShouldBeFalse();
    }

    sealed class HeaderCapturingHandler : HttpMessageHandler
    {
        public string? Authorization { get; private set; }

        public bool ApiKeyPresent { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization?.ToString();
            ApiKeyPresent = request.Headers.Contains("X-Api-Key");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    sealed class ForbiddenHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => throw new InvalidOperationException("Host-wide handler reached CIMD.");
    }

    sealed class SingleResponseHandler : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}

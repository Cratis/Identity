// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients;
using Cratis.Identity.Clients.MetadataDocuments;
using Cratis.Identity.Configuration;
using Cratis.Identity.Hosting;
using Cratis.Identity.Protocol.Stores;
using Cratis.Identity.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenIddict.Abstractions;

namespace Cratis.Identity.for_ConfigurationApplicationStore;

public class when_using_request_metadata_snapshots : Specification
{
    const string Client = "https://client.example/noncacheable.json";

    [Fact]
    async Task should_use_detached_noncacheable_metadata_only_within_the_current_request()
    {
        var fetcher = Substitute.For<IMetadataDocumentFetcher>();
        var descriptor = MetadataDescriptor.Create(new ClientIdMetadataDocument(new Uri(Client), "First", [new Uri("https://client.example/callback")]));
        fetcher.FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>()).Returns(
            new MetadataDocumentResult(descriptor, MetadataRefusal.None, TimeSpan.Zero),
            MetadataDocumentResult.Refused(MetadataRefusal.Metadata));
        var catalog = Substitute.For<IProtocolCatalog>();
        catalog.Applications.Returns([]);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(catalog);
        services.AddSingleton(new ClientIdMetadataDocuments(fetcher, new ClientIdMetadataOptions { Enabled = true }, Substitute.For<IResourceCatalog>(), TimeProvider.System, NullLogger<ClientIdMetadataDocuments>.Instance));
        services.AddScoped<IClientIdMetadataDocuments, RequestMetadataDocuments>();
        services.AddOpenIddict().AddCore(core => core.UseConfigurationStores(metadataEnabled: true));
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        string applicationId;
        await using (var request = provider.CreateAsyncScope())
        {
            var manager = request.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
            var application = await manager.FindByClientIdAsync(Client, CancellationToken.None);
            applicationId = await manager.GetIdAsync(application, CancellationToken.None);
            var resolver = request.ServiceProvider.GetRequiredService<IClientIdMetadataDocuments>();
            (await resolver.Resolve(new(Client))).Descriptor.RedirectUris.Clear();
            var again = await manager.FindByIdAsync(applicationId, CancellationToken.None);
            (await manager.GetRedirectUrisAsync(again, CancellationToken.None)).ShouldContainOnly("https://client.example/callback");
            (await manager.GetDisplayNameAsync(again, CancellationToken.None)).ShouldEqual("First");
            await fetcher.Received(1).FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>());
        }

        await using var nextRequest = provider.CreateAsyncScope();
        var nextManager = nextRequest.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        (await nextManager.FindByIdAsync(applicationId, CancellationToken.None)).ShouldBeNull();
        (await nextManager.FindByClientIdAsync(Client, CancellationToken.None)).ShouldBeNull();
        await fetcher.Received(2).FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>());
    }
}

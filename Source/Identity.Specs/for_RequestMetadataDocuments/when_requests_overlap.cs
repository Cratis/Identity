// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients;
using Cratis.Identity.Clients.MetadataDocuments;
using Cratis.Identity.Resources;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Identity.for_RequestMetadataDocuments;

public class when_requests_overlap : Specification
{
    [Fact]
    async Task should_share_one_noncacheable_snapshot_within_a_request_but_not_between_requests()
    {
        const string Client = "https://client.example/noncacheable.json";
        var completion = new TaskCompletionSource<MetadataDocumentResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstResult = new MetadataDocumentResult(MetadataDescriptor.Create(new ClientIdMetadataDocument(new Uri(Client), "First", [new Uri("https://client.example/callback")])), MetadataRefusal.None, TimeSpan.Zero);
        var nextResult = new MetadataDocumentResult(MetadataDescriptor.Create(new ClientIdMetadataDocument(new Uri(Client), "Second", [new Uri("https://client.example/callback")])), MetadataRefusal.None, TimeSpan.Zero);
        var fetcher = Substitute.For<IMetadataDocumentFetcher>();
        fetcher.FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>()).Returns(
            _ => new ValueTask<MetadataDocumentResult>(completion.Task),
            _ => new ValueTask<MetadataDocumentResult>(nextResult));
        var documents = new ClientIdMetadataDocuments(fetcher, new ClientIdMetadataOptions { Enabled = true }, Substitute.For<IResourceCatalog>(), TimeProvider.System, NullLogger<ClientIdMetadataDocuments>.Instance);
        var firstRequest = new RequestMetadataDocuments(documents);
        var nextRequest = new RequestMetadataDocuments(documents);
        var first = firstRequest.Resolve(new(Client)).AsTask();
        var sameRequest = firstRequest.Resolve(new(Client)).AsTask();
        var otherRequest = nextRequest.Resolve(new(Client)).AsTask();
        await fetcher.Received(1).FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>());
        completion.SetResult(firstResult);
        var results = await Task.WhenAll(first, sameRequest, otherRequest);
        results[0].Descriptor.DisplayName.ShouldEqual("First");
        results[1].Descriptor.DisplayName.ShouldEqual("First");
        results[2].Descriptor.DisplayName.ShouldEqual("Second");
        results[0].Descriptor.RedirectUris.Clear();
        results[1].Descriptor.RedirectUris.Count.ShouldEqual(1);
        results[2].Descriptor.RedirectUris.Count.ShouldEqual(1);
        await fetcher.Received(2).FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>());
    }
}

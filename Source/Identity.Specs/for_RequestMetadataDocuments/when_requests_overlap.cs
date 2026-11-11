// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients;
using Cratis.Identity.Clients.MetadataDocuments;
using Cratis.Identity.Configuration;
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
        var first = firstRequest.Resolve(new(Client), TestContext.Current.CancellationToken).AsTask();
        var sameRequest = firstRequest.Resolve(new(Client), TestContext.Current.CancellationToken).AsTask();
        var otherRequest = nextRequest.Resolve(new(Client), TestContext.Current.CancellationToken).AsTask();
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

    [Fact]
    async Task should_bound_outbound_work_when_many_requests_overlap_on_a_noncacheable_document()
    {
        const string Client = "https://client.example/hostile.json";
        const int Callers = 64;
        var completion = new TaskCompletionSource<MetadataDocumentResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        static MetadataDocumentResult Result() => new(MetadataDescriptor.Create(new ClientIdMetadataDocument(new Uri(Client), "Name", [new Uri("https://client.example/callback")])), MetadataRefusal.None, TimeSpan.Zero);
        var calls = 0;
        var fetcher = Substitute.For<IMetadataDocumentFetcher>();
        fetcher.FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>()).Returns(_ => Interlocked.Increment(ref calls) == 1 ? new ValueTask<MetadataDocumentResult>(completion.Task) : new ValueTask<MetadataDocumentResult>(Result()));
        var options = new ClientIdMetadataOptions { Enabled = true, MaxFetchesPerHostPerMinute = 4, MaxConcurrentFetches = 8 };
        var documents = new ClientIdMetadataDocuments(fetcher, options, Substitute.For<IResourceCatalog>(), TimeProvider.System, NullLogger<ClientIdMetadataDocuments>.Instance);
        var pending = Enumerable.Range(0, Callers).Select(_ => new RequestMetadataDocuments(documents).Resolve(new(Client), TestContext.Current.CancellationToken).AsTask()).ToArray();
        completion.SetResult(Result());
        var results = await Task.WhenAll(pending).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        results.Length.ShouldEqual(Callers);
        calls.ShouldBeLessThanOrEqual(options.MaxFetchesPerHostPerMinute);
        results.Count(_ => _.Descriptor is not null).ShouldBeLessThanOrEqual(options.MaxFetchesPerHostPerMinute);
        results.Where(_ => _.Descriptor is null).ShouldEachConformTo(_ => _.Refusal == MetadataRefusal.RateLimited);
    }

    static async Task<MetadataDocumentResult> Stall(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        return MetadataDocumentResult.Refused(MetadataRefusal.Timeout);
    }

    [Fact]
    async Task should_stop_a_cancelled_caller_that_is_fetching_its_own_noncacheable_copy()
    {
        const string Client = "https://client.example/cancelled.json";
        var completion = new TaskCompletionSource<MetadataDocumentResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var fetcher = Substitute.For<IMetadataDocumentFetcher>();
        fetcher.FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            if (Interlocked.Increment(ref calls) == 1) return new ValueTask<MetadataDocumentResult>(completion.Task);
            second.TrySetResult();
            return new ValueTask<MetadataDocumentResult>(Stall(call.Arg<CancellationToken>()));
        });
        var documents = new ClientIdMetadataDocuments(fetcher, new ClientIdMetadataOptions { Enabled = true }, Substitute.For<IResourceCatalog>(), TimeProvider.System, NullLogger<ClientIdMetadataDocuments>.Instance);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var owner = documents.Resolve(new(Client), TestContext.Current.CancellationToken).AsTask();
        var waiter = documents.Resolve(new(Client), cancellation.Token).AsTask();
        completion.SetResult(new MetadataDocumentResult(MetadataDescriptor.Create(new ClientIdMetadataDocument(new Uri(Client), "Name", [new Uri("https://client.example/callback")])), MetadataRefusal.None, TimeSpan.Zero));
        await owner;
        await second.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiter.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
    }
}

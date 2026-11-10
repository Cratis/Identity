// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients;
using Cratis.Identity.Clients.MetadataDocuments;
using Cratis.Identity.Resources;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Identity.for_MetadataDocumentCache;

public class when_caching_documents : Specification
{
    const string Client = "https://client.example/cimd.json";

    [Fact]
    async Task should_coalesce_parallel_lookups_and_detach_mutable_results()
    {
        var fetcher = Substitute.For<IMetadataDocumentFetcher>();
        var release = new TaskCompletionSource<MetadataDocumentResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        fetcher.FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>()).Returns(_ => new ValueTask<MetadataDocumentResult>(release.Task));
        var cache = Create(fetcher);
        var requests = Enumerable.Range(0, 100).Select(_ => cache.FindAsync(new(Client), CancellationToken.None).AsTask()).ToArray();
        release.SetResult(Accepted());
        var results = await Task.WhenAll(requests);
        await fetcher.Received(1).FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>());
        results[0].RedirectUris.Clear();
        results[1].RedirectUris.Count.ShouldEqual(1);
        (await cache.FindAsync(new(Client), CancellationToken.None)).RedirectUris.Count.ShouldEqual(1);
    }

    [Fact]
    async Task should_expire_without_serving_stale_metadata_and_recover_immediately_after_repair()
    {
        var fetcher = Substitute.For<IMetadataDocumentFetcher>();
        fetcher.FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>()).Returns(Accepted(), MetadataDocumentResult.Refused(MetadataRefusal.Transport), Accepted());
        var time = new Clock();
        var cache = Create(fetcher, time: time);
        (await cache.FindAsync(new(Client), CancellationToken.None)).ShouldNotBeNull();
        time.Advance(TimeSpan.FromHours(1));
        (await cache.FindAsync(new(Client), CancellationToken.None)).ShouldBeNull();
        (await cache.FindAsync(new(Client), CancellationToken.None)).ShouldNotBeNull();
        await fetcher.Received(3).FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_revalidate_immediately_after_non_reusable_or_invalid_responses(bool invalid)
    {
        var initial = invalid ? MetadataDocumentResult.Refused(MetadataRefusal.Metadata) : Accepted() with { Lifetime = TimeSpan.Zero };
        var fetcher = Substitute.For<IMetadataDocumentFetcher>();
        fetcher.FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>()).Returns(initial, Accepted());
        var cache = Create(fetcher);
        var first = await cache.FindAsync(new(Client), CancellationToken.None);
        (first is null).ShouldEqual(invalid);
        (await cache.FindAsync(new(Client), CancellationToken.None)).ShouldNotBeNull();
        await fetcher.Received(2).FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    async Task should_bound_parallel_fetches_without_queuing_new_keys()
    {
        var fetcher = Substitute.For<IMetadataDocumentFetcher>();
        var release = new TaskCompletionSource<MetadataDocumentResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        fetcher.FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>()).Returns(_ => new ValueTask<MetadataDocumentResult>(release.Task));
        var cache = Create(fetcher, new ClientIdMetadataOptions { Enabled = true, MaxCacheEntries = 1, MaxConcurrentFetches = 1 });
        var first = cache.FindAsync(new(Client), CancellationToken.None).AsTask();
        (await cache.FindAsync(new("https://other.example/cimd.json"), CancellationToken.None)).ShouldBeNull();
        release.SetResult(Accepted());
        await first;
        await fetcher.Received(1).FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    async Task should_keep_exact_keys_and_bound_reverse_ids_to_live_entries()
    {
        var fetcher = Substitute.For<IMetadataDocumentFetcher>();
        fetcher.FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>()).Returns(call => Accepted(call.Arg<ClientId>().Value));
        var cache = Create(fetcher, new ClientIdMetadataOptions { Enabled = true, MaxCacheEntries = 1, MaxConcurrentFetches = 1 });
        var first = await cache.FindAsync(new(Client), CancellationToken.None);
        var id = new Protocol.Stores.ClientApplication(first).Id;
        cache.ClientIdFor(id).ShouldEqual(Client);
        await cache.FindAsync(new("https://client.example/other.json"), CancellationToken.None);
        cache.ClientIdFor(id).ShouldBeNull();
        cache.ClientIdFor("012345678901234567890123").ShouldBeNull();
    }

    [Fact]
    async Task should_cancel_a_waiter_without_cancelling_the_shared_fetch()
    {
        var fetcher = Substitute.For<IMetadataDocumentFetcher>();
        var release = new TaskCompletionSource<MetadataDocumentResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        fetcher.FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>()).Returns(_ => new ValueTask<MetadataDocumentResult>(release.Task));
        var cache = Create(fetcher);
        using var cancelled = new CancellationTokenSource();
        var first = cache.FindAsync(new(Client), cancelled.Token).AsTask();
        var second = cache.FindAsync(new(Client), CancellationToken.None).AsTask();
        await cancelled.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        release.SetResult(Accepted());
        (await second).ShouldNotBeNull();
    }

    [Fact]
    async Task should_bound_entries_during_a_ten_thousand_url_flood_and_not_poison_another_key()
    {
        var cache = new MetadataDocumentCache(new ClientIdMetadataOptions { MaxCacheEntries = 16 }, TimeProvider.System);
        for (var index = 0; index < 10000; index++)
        {
            var key = $"https://client.example/{index}.json";
            var entry = cache.Reserve(key, out var owner, out _);
            owner.ShouldBeTrue();
            cache.Complete(entry, Accepted(key));
            (cache.Count <= 16).ShouldBeTrue();
        }

        var good = cache.Reserve(Client, out _, out _);
        cache.Complete(good, Accepted());
        var bad = cache.Reserve("https://attacker.example/cimd.json", out _, out _);
        cache.Complete(bad, Accepted());
        (await bad.Completion.Task).Descriptor.ShouldBeNull();
        (await cache.Reserve(Client, out var refetched, out var hit).Completion.Task).Descriptor.ClientId.ShouldEqual(Client);
        refetched.ShouldBeFalse();
        hit.ShouldBeTrue();
    }

    [Fact]
    void should_bound_admission_and_allow_retry_after_the_window()
    {
        var clock = new Clock();
        var admission = new MetadataFetchAdmission(new ClientIdMetadataOptions { MaxCacheEntries = 2, MaxFetchesPerMinute = 2, MaxFetchesPerHostPerMinute = 1 }, clock);
        admission.TryEnter("first.example").ShouldBeTrue();
        admission.Exit();
        admission.TryEnter("first.example").ShouldBeFalse();
        admission.TryEnter("second.example").ShouldBeTrue();
        admission.Exit();
        admission.TryEnter("third.example").ShouldBeFalse();
        clock.Advance(TimeSpan.FromMinutes(1));
        admission.TryEnter("first.example").ShouldBeTrue();
        admission.Exit();
    }

    static ClientIdMetadataDocuments Create(IMetadataDocumentFetcher fetcher, ClientIdMetadataOptions? options = null, TimeProvider? time = null) =>
        new(fetcher, options ?? new() { Enabled = true }, Substitute.For<IResourceCatalog>(), time ?? TimeProvider.System, NullLogger<ClientIdMetadataDocuments>.Instance);

    static MetadataDocumentResult Accepted(string id = Client) => new(MetadataDescriptor.Create(new ClientIdMetadataDocument(new Uri(id), "Synthetic client", [new Uri("https://client.example/callback")])), MetadataRefusal.None, TimeSpan.FromMinutes(5));

    sealed class Clock : TimeProvider
    {
        DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan interval) => _now += interval;
    }
}

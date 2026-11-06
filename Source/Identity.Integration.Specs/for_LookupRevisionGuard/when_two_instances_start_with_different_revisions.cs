// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Operational;
using MongoDB.Driver;

namespace Cratis.Identity.Integration.for_LookupRevisionGuard;

[Collection(IdentityCollection.Name)]
public class when_two_instances_start_with_different_revisions(IdentityHost host)
{
    [Fact]
    public async Task should_allow_exactly_one_instance_to_start()
    {
        var collection = host.Database.GetCollection<LookupRevision>($"lookup-revision-race-{Guid.NewGuid():N}");
        using var start = new SemaphoreSlim(0);
        var first = Start(new LookupRevisionGuard(collection, new Keys(7)));
        var second = Start(new LookupRevisionGuard(collection, new Keys(8)));
        start.Release(2);
        var results = await Task.WhenAll(first, second);
        results.Count(result => result).ShouldEqual(1);
        (await collection.CountDocumentsAsync(FilterDefinition<LookupRevision>.Empty, cancellationToken: TestContext.Current.CancellationToken)).ShouldEqual(1L);

        async Task<bool> Start(LookupRevisionGuard guard)
        {
            await start.WaitAsync(TestContext.Current.CancellationToken);
            try
            {
                await guard.StartAsync(TestContext.Current.CancellationToken);
                return true;
            }
            catch (LookupKeyRevisionMismatch)
            {
                return false;
            }
        }
    }

    sealed class Keys(int revision) : IUpstreamLookupKeys
    {
        public UpstreamLookupKey Current { get; } = new(revision, Enumerable.Range(0, 32).Select(value => (byte)value).ToArray());
    }
}

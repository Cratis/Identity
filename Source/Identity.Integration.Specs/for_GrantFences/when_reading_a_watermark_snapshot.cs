// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using Cratis.Identity.Revocation.Operational;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace Cratis.Identity.Integration.for_GrantFences;

[Collection(IdentityCollection.Name)]
public class when_reading_a_watermark_snapshot(IdentityHost host) : IAsyncDisposable
{
    readonly List<IMongoDatabase> _databases = [];

    [Fact]
    public async Task should_keep_entries_and_counter_consistent_when_a_reservation_commits_between_reads()
    {
        var database = await Database();
        var writer = new MongoIndependentFenceLog(database, TimeProvider.System);
        var id = Guid.NewGuid();
        var sequence = await writer.Reserve(id, "completed-grant", CancellationToken.None);
        var reservation = new FenceReservation(id, sequence, "completed-grant", ReservationState.Reserved, DateTimeOffset.UnixEpoch);
        await writer.Fence(reservation, CancellationToken.None);
        await writer.Resolve(reservation, null, CancellationToken.None);

        var enumerated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var settings = database.Client.Settings.Clone();
        settings.ClusterConfigurator += cluster => cluster.Subscribe<CommandSucceededEvent>(command =>
        {
            if (command.CommandName == "find" && command.Reply.TryGetValue("cursor", out var cursor) &&
                cursor.AsBsonDocument["ns"].AsString == $"{database.DatabaseNamespace.DatabaseName}.fence-reservations")
            {
                // The server has enumerated the entries in this transaction, but the counter read cannot start yet.
                enumerated.TrySetResult();

                // Bounded wait; the spec body observes a missing release as a failed assertion, not a driver-thread exception.
                resume.Task.Wait(TimeSpan.FromSeconds(30));
            }
        });
        using var readerClient = new MongoClient(settings);
        var reader = new MongoIndependentFenceLog(readerClient.GetDatabase(database.DatabaseNamespace.DatabaseName), TimeProvider.System);
        var watermark = Task.Run(() => reader.Watermark(CancellationToken.None));
        try
        {
            var first = await Task.WhenAny(enumerated.Task, watermark).WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
            if (first == watermark)
            {
                // The read finished before pausing; surface its failure or the missed pause.
                await watermark;
                Assert.Fail("The watermark read completed before the entry enumeration was observed.");
            }

            await writer.Reserve(Guid.NewGuid(), "pending-grant", CancellationToken.None);
        }
        finally
        {
            resume.TrySetResult();
        }

        (await watermark).ShouldEqual(1L);

        // A fresh read sees the new reservation but must not advance over its unresolved restriction.
        (await writer.AllocatedThrough(CancellationToken.None)).ShouldEqual(2L);
        (await writer.Watermark(CancellationToken.None)).ShouldEqual(1L);
    }

    [Theory]
    [InlineData(2L, 1L)] // Truncated entries: counter ahead.
    [InlineData(0L, 1L)] // Counter behind a stable entry.
    [InlineData(2L, 2L)] // Missing sequence one, even though the counter matches the last sequence.
    public async Task should_reject_corruption_in_a_stable_snapshot(long allocated, long sequence)
    {
        var database = await Database();
        await database.GetCollection<BsonDocument>("fence-custody").UpdateOneAsync(
            new BsonDocument("_id", "metadata"), new BsonDocument("$set", new BsonDocument("allocated", allocated)), cancellationToken: TestContext.Current.CancellationToken);
        await database.GetCollection<BsonDocument>("fence-reservations").InsertOneAsync(
            new BsonDocument
            {
                { "_id", Guid.NewGuid().ToString("D") }, { "sequence", sequence }, { "state", (int)ReservationState.Voided }
            },
            cancellationToken: TestContext.Current.CancellationToken);
        var log = new MongoIndependentFenceLog(database, TimeProvider.System);

        await Assert.ThrowsAsync<RevocationPending>(() => log.Watermark(CancellationToken.None));
    }

    async Task<IMongoDatabase> Database()
    {
        var independent = host.Services.GetRequiredService<IndependentFenceLogConnection>().Database;
        var database = independent.Client.GetDatabase($"watermark-test-{Guid.NewGuid():N}");
        _databases.Add(database);
        await database.GetCollection<BsonDocument>("fence-custody").InsertOneAsync(new BsonDocument
        {
            { "_id", "metadata" }, { "instanceId", Guid.NewGuid().ToString("D") }, { "allocated", 0L }, { "epoch", 0L }
        });
        return database;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        foreach (var database in _databases)
        {
            await database.Client.DropDatabaseAsync(database.DatabaseNamespace.DatabaseName);
        }

        GC.SuppressFinalize(this);
    }
}

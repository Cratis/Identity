// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts.Lookup;
using MongoDB.Driver;

namespace Cratis.Identity.Operational.for_LookupRevisionGuard.given;

public class a_revision_guard : Specification
{
    private protected UpstreamLookupKey _key;
    private protected IMongoCollection<LookupRevision> _collection;
    protected Exception _error;
    LookupRevisionGuard _guard;

    protected int Writes => _collection.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IMongoCollection<LookupRevision>.InsertOneAsync) || call.GetMethodInfo().Name == nameof(IMongoCollection<LookupRevision>.FindOneAndUpdateAsync));
    protected int Reads => _collection.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IMongoCollection<LookupRevision>.FindAsync));
    protected int Inserts => _collection.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IMongoCollection<LookupRevision>.InsertOneAsync));

    void Establish()
    {
        _key = new(7, Enumerable.Range(0, 32).Select(value => (byte)value).ToArray());
        var keys = Substitute.For<IUpstreamLookupKeys>();
        keys.Current.Returns(_key);
        _collection = Substitute.For<IMongoCollection<LookupRevision>>();
        _collection.FindOneAndUpdateAsync(Arg.Any<FilterDefinition<LookupRevision>>(), Arg.Any<UpdateDefinition<LookupRevision>>(), Arg.Any<FindOneAndUpdateOptions<LookupRevision, LookupRevision>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LookupRevision(LookupRevision.SingletonId, _key.Check, _key.Revision)));
        _guard = new(_collection, keys);
    }

    private protected void Stored(LookupRevision[] before, LookupRevision[] after)
    {
        var beforeCursor = Cursor(before);
        var afterCursor = Cursor(after);
        _collection.FindAsync(Arg.Any<FilterDefinition<LookupRevision>>(), Arg.Any<FindOptions<LookupRevision, LookupRevision>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(beforeCursor), Task.FromResult(afterCursor));
    }

    protected async Task Start()
    {
        try
        {
            await _guard.StartAsync(CancellationToken.None);
        }
        catch (LookupKeyRevisionMismatch exception)
        {
            _error = exception;
        }
    }

    static IAsyncCursor<LookupRevision> Cursor(LookupRevision[] rows)
    {
        var cursor = Substitute.For<IAsyncCursor<LookupRevision>>();
        cursor.Current.Returns(rows);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
        return cursor;
    }
}

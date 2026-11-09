// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Revocation;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using OpenIddict.MongoDb;
using OpenIddict.MongoDb.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.RevocationSpecs.given;

public class an_epoch_store : Specification
{
    private protected readonly RecoveryGate _gate = new();
    private protected FencedTokenStore _tokens;
    private protected FencedAuthorizationStore _authorizations;
    protected readonly EpochCollection<OpenIddictMongoDbToken> _tokenRows = new();
    protected readonly EpochCollection<OpenIddictMongoDbAuthorization> _authorizationRows = new();
    protected readonly OpenIddictMongoDbToken _token = new() { Id = ObjectId.GenerateNewId(), Type = TokenTypeHints.AccessToken };
    protected readonly OpenIddictMongoDbAuthorization _authorization = new() { Id = ObjectId.GenerateNewId() };

    async Task Establish()
    {
        var options = new OpenIddictMongoDbOptions();
        var database = Substitute.For<IMongoDatabase>();
        database.GetCollection<OpenIddictMongoDbToken>(options.TokensCollectionName, Arg.Any<MongoCollectionSettings>()).Returns(_tokenRows.Collection);
        database.GetCollection<OpenIddictMongoDbAuthorization>(options.AuthorizationsCollectionName, Arg.Any<MongoCollectionSettings>()).Returns(_authorizationRows.Collection);
        var context = Substitute.For<IOpenIddictMongoDbContext>();
        context.GetDatabaseAsync(Arg.Any<CancellationToken>()).Returns(database);
        var monitor = Substitute.For<IOptionsMonitor<OpenIddictMongoDbOptions>>();
        monitor.CurrentValue.Returns(options);
        _gate.ObserveEpoch(7);
        _gate.Open(true);
        _tokens = new(context, monitor, new SharedFenceStore(), new RefreshIssuance(), _gate);
        _authorizations = new(context, monitor, _gate);
        await _tokens.CreateAsync(_token, CancellationToken.None);
        await _authorizations.CreateAsync(_authorization, CancellationToken.None);
    }

    public class EpochCollection<T>
        where T : class
    {
        public readonly IMongoCollection<T> Collection = Substitute.For<IMongoCollection<T>>();
        public T Entry;
        internal readonly FaultPoints Faults = new();

        public EpochCollection()
        {
            Collection.InsertOneAsync(Arg.Do<T>(Capture), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            Collection.ReplaceOneAsync(Arg.Any<FilterDefinition<T>>(), Arg.Do<T>(Capture), Arg.Any<ReplaceOptions>(), Arg.Any<CancellationToken>())
                .Returns(new ReplaceOneResult.Acknowledged(1, 1, null));
            Collection.FindAsync(Arg.Any<FilterDefinition<T>>(), Arg.Any<FindOptions<T, T>>(), Arg.Any<CancellationToken>()).Returns(_ =>
            {
                var cursor = Substitute.For<IAsyncCursor<T>>();
                cursor.Current.Returns([Entry]);
                cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true, false);
                return cursor;
            });
        }

        void Capture(T entry)
        {
            Entry = BsonSerializer.Deserialize<T>(entry.ToBson());
            Faults.Reach(FaultPoint.AfterCommit);
        }
    }
}

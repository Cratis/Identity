// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Device;
using Cratis.Identity.Storage;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using OpenIddict.MongoDb;
using OpenIddict.MongoDb.Models;

namespace Cratis.Identity.for_StorageIndexes;

public class when_ensuring_the_indexes : Specification
{
    IMongoDatabase _database;
    IMongoIndexManager<OpenIddictMongoDbToken> _tokenIndexes;
    List<CreateIndexModel<OpenIddictMongoDbToken>> _models;
    List<CreateIndexModel<DeviceGrantState>> _deviceModels;
    List<CreateIndexModel<BsonDocument>> _replayModels;

    void Establish()
    {
        var options = new OpenIddictMongoDbOptions();
        var monitor = Substitute.For<IOptionsMonitor<OpenIddictMongoDbOptions>>();
        monitor.CurrentValue.Returns(options);

        _deviceModels = [];
        var deviceIndexes = Substitute.For<IMongoIndexManager<DeviceGrantState>>();
        deviceIndexes.CreateOneAsync(Arg.Do<CreateIndexModel<DeviceGrantState>>(_ => _deviceModels.Add(_)), Arg.Any<CreateOneIndexOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("device-expiry"));
        var deviceStates = Substitute.For<IMongoCollection<DeviceGrantState>>();
        deviceStates.Indexes.Returns(deviceIndexes);
        _models = [];
        _tokenIndexes = Substitute.For<IMongoIndexManager<OpenIddictMongoDbToken>>();
        _tokenIndexes.CreateManyAsync(Arg.Do<IEnumerable<CreateIndexModel<OpenIddictMongoDbToken>>>(_ => _models.AddRange(_)), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<string>>([]));
        var tokens = Substitute.For<IMongoCollection<OpenIddictMongoDbToken>>();
        tokens.Indexes.Returns(_tokenIndexes);

        _database = Substitute.For<IMongoDatabase>();
        _database.GetCollection<DeviceGrantState>("device_grant_state", Arg.Any<MongoCollectionSettings>()).Returns(deviceStates);
        _database.GetCollection<OpenIddictMongoDbToken>(options.TokensCollectionName, Arg.Any<MongoCollectionSettings>()).Returns(tokens);
        _replayModels = [];
        var replayIndexes = Substitute.For<IMongoIndexManager<BsonDocument>>();
        replayIndexes.CreateOneAsync(Arg.Do<CreateIndexModel<BsonDocument>>(_ => _replayModels.Add(_)), Arg.Any<CreateOneIndexOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult("subject_token_replay_expiry"));
        var replays = Substitute.For<IMongoCollection<BsonDocument>>();
        replays.Indexes.Returns(replayIndexes);
        _database.GetCollection<BsonDocument>("subject-token-replays", Arg.Any<MongoCollectionSettings>()).Returns(replays);
        _indexes = new StorageIndexes(_database, monitor, Options.Create(new IdentityOptions { DeviceGrant = new() { Enabled = true } }));
    }

    StorageIndexes _indexes;

    async Task Because() => await _indexes.Ensure(CancellationToken.None);

    CreateIndexModel<OpenIddictMongoDbToken> Ttl => _models.Single(_ => _.Options?.ExpireAfter is not null);

    [Fact] void should_create_exactly_one_device_expiry_index() => _deviceModels.Count.ShouldEqual(1);
    [Fact] void should_expire_device_state_at_its_logical_expiration() => _deviceModels.Single().Options.ExpireAfter.ShouldEqual(TimeSpan.Zero);
    [Fact] void should_index_device_state_expiration() => _deviceModels.Single().Keys.Render(new RenderArgs<DeviceGrantState>(BsonSerializer.LookupSerializer<DeviceGrantState>(), BsonSerializer.SerializerRegistry)).ToString().ShouldContain("ExpiresAt");

    [Fact] void should_not_request_an_operational_accounts_collection() => _database.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(IMongoDatabase.GetCollection) && Equals(call.GetArguments()[0], "accounts")).ShouldBeFalse();
    [Fact] void should_not_request_application_or_scope_collections() => _database.ReceivedCalls().Where(call => call.GetMethodInfo().Name == nameof(IMongoDatabase.GetCollection)).Select(call => (string)call.GetArguments()[0]).Order().ShouldEqual(new[] { "browser_sessions", "consent-requests", "device_grant_state", "grant-fences", "grant-fence-log", "subject-token-replays", new OpenIddictMongoDbOptions().AuthorizationsCollectionName, new OpenIddictMongoDbOptions().TokensCollectionName }.Order());
    [Fact] void should_create_one_replay_expiry_index() => _replayModels.Count.ShouldEqual(1);
    [Fact] void should_expire_replays_at_their_retention_deadline() => _replayModels.Single().Options.ExpireAfter.ShouldEqual(TimeSpan.Zero);
    [Fact] void should_index_the_replay_retention_field() => _replayModels.Single().Keys.Render(new RenderArgs<BsonDocument>(BsonSerializer.LookupSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry)).ShouldEqual(new BsonDocument("Until", 1));
    [Fact] void should_create_one_ttl_index() => _models.Count(_ => _.Options?.ExpireAfter is not null).ShouldEqual(1);
    [Fact] void should_expire_entries_at_their_expiration_date() => Ttl.Options.ExpireAfter.ShouldEqual(TimeSpan.Zero);
    [Fact] void should_index_the_expiration_date() => Ttl.Keys.Render(new RenderArgs<OpenIddictMongoDbToken>(BsonSerializer.LookupSerializer<OpenIddictMongoDbToken>(), BsonSerializer.SerializerRegistry)).ToString().ShouldContain("expiration_date");
}

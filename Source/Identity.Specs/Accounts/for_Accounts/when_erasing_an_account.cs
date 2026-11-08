// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Identity.Accounts.for_Accounts;

public class when_erasing_an_account : Specification
{
    static readonly DateTimeOffset _now = new(2026, 11, 7, 10, 0, 0, TimeSpan.Zero);

    IMongoCollection<AccountDocument> _accounts;
    IMongoCollection<ErasedAccountDocument> _erased;
    FilterDefinition<AccountDocument> _deleteFilter;
    FilterDefinition<ErasedAccountDocument> _markerFilter;
    UpdateDefinition<ErasedAccountDocument> _markerUpdate;
    UpdateOptions _markerOptions;
    Accounts _store;

    void Establish()
    {
        _accounts = Substitute.For<IMongoCollection<AccountDocument>>();
        _accounts.DeleteOneAsync(Arg.Do<FilterDefinition<AccountDocument>>(_ => _deleteFilter = _), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DeleteResult>(new DeleteResult.Acknowledged(1)));
        _erased = Substitute.For<IMongoCollection<ErasedAccountDocument>>();
        _erased.UpdateOneAsync(
            Arg.Do<FilterDefinition<ErasedAccountDocument>>(_ => _markerFilter = _),
            Arg.Do<UpdateDefinition<ErasedAccountDocument>>(_ => _markerUpdate = _),
            Arg.Do<UpdateOptions>(_ => _markerOptions = _),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(0, 0, null)));

        var database = Substitute.For<IMongoDatabase>();
        database.GetCollection<AccountDocument>(Accounts.CollectionName, Arg.Any<MongoCollectionSettings>()).Returns(_accounts);
        database.GetCollection<ErasedAccountDocument>(Accounts.ErasedCollectionName, Arg.Any<MongoCollectionSettings>()).Returns(_erased);
        _store = new Accounts(database, new FixedTimeProvider(_now));
    }

    async Task Because() => await _store.Erase("account-1", CancellationToken.None);

    static string Render<T>(FilterDefinition<T> filter) => filter.Render(new RenderArgs<T>(BsonSerializer.LookupSerializer<T>(), BsonSerializer.SerializerRegistry)).ToString();

    static string Render<T>(UpdateDefinition<T> update) => update.Render(new RenderArgs<T>(BsonSerializer.LookupSerializer<T>(), BsonSerializer.SerializerRegistry)).ToString()!;

    [Fact] void should_delete_the_account_document_by_id() => Render(_deleteFilter).ShouldContain("account-1");
    [Fact] void should_write_the_marker_for_the_account() => Render(_markerFilter).ShouldContain("account-1");
    [Fact] void should_write_the_marker_as_an_upsert() => _markerOptions.IsUpsert.ShouldBeTrue();
    [Fact] void should_only_set_the_erasure_time_on_insert() => Render(_markerUpdate).ShouldContain("$setOnInsert");
    [Fact] void should_keep_no_personal_field_in_the_marker() => Render(_markerUpdate).ShouldNotContain("github");
}

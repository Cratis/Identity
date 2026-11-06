// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;
using OpenIddict.MongoDb.Models;

namespace Cratis.Identity.Clients.for_ClientMaterializations;

public class when_an_older_write_arrives_after_a_rotation : given.a_materialization
{
    Exception _error;

    async Task Establish()
    {
        await _sink.Upsert(_state with { Version = new(13), CurrentSecretHash = new("v1.newer-secret-comparand") }, _policy, CancellationToken.None);
        _collection.When(collection => collection.ReplaceOneAsync(Arg.Any<FilterDefinition<OpenIddictMongoDbApplication>>(), Arg.Any<OpenIddictMongoDbApplication>(), Arg.Any<ReplaceOptions>(), Arg.Any<CancellationToken>())).Do(call =>
        {
            var filter = Render(call.Arg<FilterDefinition<OpenIddictMongoDbApplication>>());
            var maximum = filter["$or"][1]["properties.cratis_client_registration_version"]["$lte"].AsDecimal128;
            if (_document.Properties!["cratis_client_registration_version"].AsDecimal128 > maximum)
            {
                // Mongo's atomic conditional upsert cannot insert a second document with the existing _id.
                throw new MongoException("Duplicate _id for an application with a newer version.");
            }
        });
    }

    async Task Because() => _error = await Catch.Exception(async () => await _sink.Upsert(_state, _policy, CancellationToken.None));

    [Fact] void should_refuse_the_stale_conditional_upsert() => _error.ShouldBeOfExactType<MongoException>();
    [Fact] void should_preserve_the_newer_secret() => _document.ClientSecret.ShouldEqual("v1.newer-secret-comparand");
    [Fact] void should_preserve_the_newer_version() => ((decimal)_document.Properties!["cratis_client_registration_version"].AsDecimal128).ShouldEqual(13m);
}

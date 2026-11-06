// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Storage;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using OpenIddict.Abstractions;
using OpenIddict.MongoDb.Models;

namespace Cratis.Identity.Clients.given;

public class a_sweeping_client_store : a_client_registration_store
{
    protected Dictionary<ObjectId, OpenIddictMongoDbApplication> _applications;

    void Establish()
    {
        _applications = [];
        var database = Substitute.For<IMongoDatabase>();
        var collection = Substitute.For<IMongoCollection<OpenIddictMongoDbApplication>>();
        database.GetCollection<OpenIddictMongoDbApplication>("openiddict.applications").Returns(collection);
        collection.DeleteManyAsync(default!, default).ReturnsForAnyArgs(call =>
        {
            var filter = call.Arg<FilterDefinition<OpenIddictMongoDbApplication>>().Render(new RenderArgs<OpenIddictMongoDbApplication>(BsonSerializer.SerializerRegistry.GetSerializer<OpenIddictMongoDbApplication>(), BsonSerializer.SerializerRegistry));
            var active = filter["_id"]["$nin"].AsBsonArray.Select(id => id.AsObjectId).ToHashSet();
            var stale = _applications.Keys.Except(active).ToArray();
            foreach (var id in stale)
            {
                _applications.Remove(id);
            }

            return Task.FromResult<DeleteResult>(new DeleteResult.Acknowledged(stale.Length));
        });
        var sink = new ClientMaterializations(database);
        _materializations.Sweep(default!, default).ReturnsForAnyArgs(call => sink.Sweep(call.Arg<IEnumerable<ClientRegistrationId>>(), call.Arg<CancellationToken>()));
        _materializations.When(sink => sink.Upsert(Arg.Any<Registration.ClientRegistrationState>(), Arg.Any<OpenIddictApplicationDescriptor>(), Arg.Any<CancellationToken>())).Do(call =>
        {
            var state = call.Arg<Registration.ClientRegistrationState>();
            var id = ClientMaterializations.ApplicationId(state.ClientRegistrationId);
            _applications[id] = new()
            {
                Id = id,
                ClientId = state.ClientId!.Value,
                Properties = new BsonDocument("cratis_client_registration_id", state.Id)
            };
        });
    }
}

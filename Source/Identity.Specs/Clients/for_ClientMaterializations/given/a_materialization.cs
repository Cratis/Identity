// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients.Registration;
using Cratis.Identity.Clients.Secrets;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Storage;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using OpenIddict.Abstractions;
using OpenIddict.MongoDb.Models;

namespace Cratis.Identity.Clients.for_ClientMaterializations.given;

public class a_materialization : Specification
{
    protected IMongoCollection<OpenIddictMongoDbApplication> _collection;
    protected ClientMaterializations _sink;
    protected ClientRegistrationState _state;
    protected OpenIddictApplicationDescriptor _policy;
    protected OpenIddictMongoDbApplication _document;
    protected List<string> _operations;
    protected BsonDocument _deleted;
    protected BsonDocument _replaced;
    protected ReplaceOptions _replaceOptions;

    void Establish()
    {
        var database = Substitute.For<IMongoDatabase>();
        _collection = Substitute.For<IMongoCollection<OpenIddictMongoDbApplication>>();
        database.GetCollection<OpenIddictMongoDbApplication>("openiddict.applications").Returns(_collection);
        _sink = new(database);
        _operations = [];
        _collection.DeleteManyAsync(default!, default).ReturnsForAnyArgs(call =>
        {
            _operations.Add("delete-conflicting");
            _deleted = Render(call.Arg<FilterDefinition<OpenIddictMongoDbApplication>>());
            return Task.FromResult<DeleteResult>(new DeleteResult.Acknowledged(1));
        });
        _collection.ReplaceOneAsync(default!, default!, default(ReplaceOptions)!, default).ReturnsForAnyArgs(call =>
        {
            _operations.Add("upsert");
            _document = call.Arg<OpenIddictMongoDbApplication>();
            _replaced = Render(call.Arg<FilterDefinition<OpenIddictMongoDbApplication>>());
            _replaceOptions = call.Arg<ReplaceOptions>();
            return Task.FromResult<ReplaceOneResult>(new ReplaceOneResult.Acknowledged(1, 1, null));
        });
        _collection.DeleteOneAsync(default!, default).ReturnsForAnyArgs(call =>
        {
            _operations.Add("remove");
            _deleted = Render(call.Arg<FilterDefinition<OpenIddictMongoDbApplication>>());
            return Task.FromResult<DeleteResult>(new DeleteResult.Acknowledged(1));
        });
        _state = new("00112233-4455-4677-8899-aabbccddeeff")
        {
            ClientId = ClientId.Create("introspection"),
            Version = new(12),
            RedirectUris = [RedirectUri.Create("https://fact.example/callback")],
            CurrentSecretHash = new ClientSecretDigester(Clients.given.client_test_keys.Create()).Digest(ClientId.Create("introspection"), "never-store-this-raw-secret")
        };
        _policy = new()
        {
            ClientId = "wrong-policy-id",
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
            ClientSecret = "never-store-this-raw-secret",
            ApplicationType = OpenIddictConstants.ApplicationTypes.Web,
            ConsentType = OpenIddictConstants.ConsentTypes.Explicit,
            DisplayName = "policy display"
        };
        _policy.RedirectUris.Add(new("https://wrong-policy.example/callback"));
        _policy.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Introspection);
        _policy.Requirements.Add(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);
    }

    protected static BsonDocument Render(FilterDefinition<OpenIddictMongoDbApplication> filter) => filter.Render(new RenderArgs<OpenIddictMongoDbApplication>(BsonSerializer.SerializerRegistry.GetSerializer<OpenIddictMongoDbApplication>(), BsonSerializer.SerializerRegistry));
}

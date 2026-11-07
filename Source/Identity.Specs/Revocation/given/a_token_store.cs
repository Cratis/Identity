// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using OpenIddict.MongoDb;
using OpenIddict.MongoDb.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.RevocationSpecs.given;

public class a_token_store : Specification
{
    private protected FencedTokenStore _store;
    private protected SharedFenceStore _fences;
    private protected RefreshIssuance _issuance;
    protected IMongoCollection<OpenIddictMongoDbToken> _collection;
    protected OpenIddictMongoDbToken _entry;

    void Establish()
    {
        _collection = Substitute.For<IMongoCollection<OpenIddictMongoDbToken>>();
        var database = Substitute.For<IMongoDatabase>();
        var options = new OpenIddictMongoDbOptions();
        database.GetCollection<OpenIddictMongoDbToken>(options.TokensCollectionName, Arg.Any<MongoCollectionSettings>()).Returns(_collection);
        var context = Substitute.For<IOpenIddictMongoDbContext>();
        context.GetDatabaseAsync(Arg.Any<CancellationToken>()).Returns(database);
        var monitor = Substitute.For<IOptionsMonitor<OpenIddictMongoDbOptions>>();
        monitor.CurrentValue.Returns(options);
        _fences = new SharedFenceStore();
        _issuance = new RefreshIssuance();
        _store = new(context, monitor, _fences, _issuance);
        _entry = new OpenIddictMongoDbToken
        {
            Id = ObjectId.Parse("65ab56ef0123456789012345"),
            AuthorizationId = ObjectId.Parse("65ab56ef0123456789012346"),
            Type = TokenTypeIdentifiers.RefreshToken,
            Subject = FenceDoubles.Subject,
            CreationDate = DateTime.UnixEpoch,
            ExpirationDate = DateTime.UnixEpoch.AddDays(30)
        };
    }
}

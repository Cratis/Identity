// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Integration.for_TokenStorage.given;

/// <summary>
/// Changes what is stored behind Identity's back.
/// </summary>
public static class ExpiredEntries
{
    /// <summary>
    /// The type OpenIddict stores for an authorization code entry (the token type identifier, not a short name).
    /// </summary>
    public const string AuthorizationCode = TokenTypeIdentifiers.Private.AuthorizationCode;

    /// <summary>
    /// The type OpenIddict stores for a refresh token entry.
    /// </summary>
    public const string RefreshToken = TokenTypeIdentifiers.RefreshToken;

    /// <summary>
    /// Sets the expiry of every stored token of a type to the past, as if the TTL monitor had not yet deleted them.
    /// The BSON names are those of OpenIddict.MongoDb.Models 7.8.0 (<c language="text">type</c>, <c language="text">expiration_date</c>).
    /// Fails when no entry of the type exists, so a wrong name or value cannot make a spec pass vacuously.
    /// </summary>
    /// <param name="host">The <see cref="IdentityHost"/>.</param>
    /// <param name="type">The stored token type.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task Expire(IdentityHost host, string type)
    {
        var tokens = host.Database.GetCollection<BsonDocument>("openiddict.tokens");
        var filter = Builders<BsonDocument>.Filter.Eq("type", type);
        var before = await tokens.CountDocumentsAsync(filter);
        Assert.True(before > 0, $"No token entry of type {type} is stored; the filter does not match what OpenIddict stores.");

        var past = DateTime.UtcNow.AddMinutes(-5);
        var result = await tokens.UpdateManyAsync(filter, Builders<BsonDocument>.Update.Set("expiration_date", past));
        Assert.True(result.MatchedCount > 0 && result.ModifiedCount > 0, "No token entry was changed to expired.");
        Assert.True(
            await tokens.CountDocumentsAsync(Builders<BsonDocument>.Filter.And(filter, Builders<BsonDocument>.Filter.Lt("expiration_date", DateTime.UtcNow))) > 0,
            "The expired entry cannot be read back.");
    }
}

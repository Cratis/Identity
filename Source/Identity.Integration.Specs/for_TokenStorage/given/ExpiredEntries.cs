// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Identity.Integration.for_TokenStorage.given;

/// <summary>
/// Changes what is stored behind Identity's back.
/// </summary>
public static class ExpiredEntries
{
    /// <summary>
    /// Sets the expiry of every stored token of a type to the past, as if the TTL monitor had not yet deleted them.
    /// </summary>
    /// <param name="host">The <see cref="IdentityHost"/>.</param>
    /// <param name="type">The token type.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task Expire(IdentityHost host, string type)
    {
        var tokens = host.Database.GetCollection<BsonDocument>("openiddict.tokens");
        var result = await tokens.UpdateManyAsync(
            Builders<BsonDocument>.Filter.Eq("type", type),
            Builders<BsonDocument>.Update.Set("expiration_date", DateTime.UtcNow.AddMinutes(-5)));
        result.MatchedCount.ShouldBeGreaterThan(0);
    }
}

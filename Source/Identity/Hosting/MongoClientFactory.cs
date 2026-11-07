// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Identity.Hosting;

/// <summary>
/// Creates the <see cref="IMongoClient"/> from the configured connection string without letting a parse failure
/// carry the string into the host's startup logs.
/// </summary>
internal static class MongoClientFactory
{
    /// <summary>
    /// Creates the client.
    /// </summary>
    /// <param name="connectionString">The configured connection string.</param>
    /// <returns>The <see cref="IMongoClient"/>.</returns>
    /// <exception cref="InvalidMongoDBConnectionString">Thrown when the connection string cannot be parsed.</exception>
    internal static IMongoClient Create(string connectionString)
    {
        MongoClientSettings settings;
        try
        {
            settings = MongoClientSettings.FromConnectionString(connectionString);
        }
#pragma warning disable CA1031 // The driver's exception type and message vary and can quote the string; none of it may escape.
        catch (Exception)
#pragma warning restore CA1031
        {
            // Deliberately no inner exception: it would carry the driver's message into the log.
            throw new InvalidMongoDBConnectionString();
        }

        return new MongoClient(settings);
    }
}

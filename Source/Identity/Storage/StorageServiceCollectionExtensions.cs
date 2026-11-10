// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Cratis.Identity.Hosting;
using Cratis.Identity.Operational;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenIddict.MongoDb;

namespace Cratis.Identity.Storage;

/// <summary>Registers operational storage and its ordered startup guards.</summary>
internal static class StorageServiceCollectionExtensions
{
    /// <summary>Adds operational storage.</summary>
    /// <param name="services">The services.</param>
    internal static void AddStorage(this IServiceCollection services)
    {
        // Must stay first: hosted services start in registration order and nothing may write to MongoDB before the guard has looked.
        services.AddLegacyAccountStoreGuard();
        services.AddSingleton(sp => MongoClientFactory.Create(sp.GetRequiredService<IOptions<IdentityOptions>>().Value.MongoDB.ConnectionString));
        services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(sp.GetRequiredService<IOptions<IdentityOptions>>().Value.MongoDB.Database));
        services.AddSingleton<StorageIndexes>();

        services.AddHostedService<StorageIndexInitialization>();
        services.AddSingleton<IOpenIddictMongoDbContext, OpenIddictMongoDbContext>();
        services.AddOptions<OpenIddictMongoDbOptions>();
        services.AddHostedService<TokenPruning>();
    }
}

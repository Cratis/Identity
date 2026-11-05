// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Clients.Events;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using MongoDB.Bson;
using MongoDB.Driver;
using OpenIddict.MongoDb.Models;
using Testcontainers.MongoDb;

namespace Cratis.Identity.Integration.for_ClientRegistrations.given;

/// <summary>One real Mongo and pinned Chronicle kernel per scenario, never started locally by the gate.</summary>
public class ClientRegistrationFixture : IAsyncLifetime
{
    public const string InitialSecret = "initial-client-secret-hostile-marker-37";
    public const string RotatedSecret = "rotated-client-secret-hostile-marker-37";
    readonly INetwork _network = new NetworkBuilder().Build();
    readonly SecretStoreDirectory _secrets = new();
    readonly string _database = $"identity-clients-{Guid.NewGuid():N}";
    readonly MongoDbContainer _mongo;
    readonly IContainer _kernel;

    public ClientRegistrationFixture()
    {
        _mongo = new MongoDbBuilder("mongo:8.0.14")
            .WithUsername("spec")
            .WithPassword("spec-password")
            .WithNetwork(_network)
            .WithNetworkAliases("client-registration-mongo")
            .Build();
        _kernel = new ContainerBuilder("cratis/chronicle:19.111.0")
            .WithNetwork(_network)
            .WithEnvironment("Storage__ConnectionDetails", "mongodb://spec:spec-password@client-registration-mongo:27017/?authSource=admin")
            .WithPortBinding(35000, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(35000).ForPath("/health").UsingTls()))
            .Build();
        _secrets.WriteKey("signing", "clients", DateTimeOffset.UtcNow.AddHours(-1));
        _secrets.WriteKey("encryption", "clients", DateTimeOffset.UtcNow.AddHours(-1));
        _secrets.WriteDataProtectionKey();
    }

    public ClientRegistrationIdentityHost Host { get; private set; } = null!;

    public IMongoDatabase Database => new MongoClient(_mongo.GetConnectionString()).GetDatabase(_database);

    public IMongoCollection<OpenIddictMongoDbApplication> Applications => Database.GetCollection<OpenIddictMongoDbApplication>("openiddict.applications");

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        await _network.CreateAsync();
        await _mongo.StartAsync();
        await _kernel.StartAsync();
        await Restart();
    }

    /// <summary>Restarts Identity only, preserving the kernel, event log, operational database and key custody.</summary>
    /// <param name="secret">The configured introspection secret, or null to remove that declaration.</param>
    /// <returns>The restart task.</returns>
    public async Task Restart(string? secret = InitialSecret)
    {
        if (Host is not null)
        {
            await Host.DisposeAsync();
        }

        var settings = new Dictionary<string, string?>
        {
            ["Identity:Issuer"] = IdentityHost.Issuer,
            ["Identity:MongoDB:ConnectionString"] = _mongo.GetConnectionString(),
            ["Identity:MongoDB:Database"] = _database,
            ["Identity:Chronicle:ConnectionString"] = $"chronicle://{_kernel.Hostname}:{_kernel.GetMappedPublicPort(35000)}?skipTlsValidation=true",
            ["Identity:Clients:EventSourced"] = "true",
            ["Identity:Clients:SecretDigestKey"] = Convert.ToBase64String(Enumerable.Repeat((byte)37, 32).ToArray()),
            ["Identity:SecretStore:Path"] = _secrets.Path,
            ["Identity:GitHub:ClientId"] = "unused-in-specs",
            ["Identity:GitHub:ClientSecret"] = "unused-in-specs",
            ["Identity:Resources:0:Tenants:0:Id"] = IdentityHost.MemberTenant,
            ["Identity:Resources:0:Tenants:0:Name"] = "Cratis",
            ["Identity:Resources:0:Tenants:0:AllowAllAccounts"] = "true"
        };
        if (secret is not null)
        {
            settings["Identity:IntrospectionClients:0:ClientId"] = IdentityHost.IntrospectionClientId;
            settings["Identity:IntrospectionClients:0:ClientSecret"] = secret;
        }

        Host = new(settings);
        _ = Host.Server;
    }

    /// <summary>Reads actual stored event documents, not generated schemas or reconstructed CLR payloads.</summary>
    /// <returns>The persisted client facts across Chronicle databases.</returns>
    public async Task<IReadOnlyList<BsonDocument>> ClientFacts()
    {
        var mongo = new MongoClient(_mongo.GetConnectionString());
        using var databases = await mongo.ListDatabaseNamesAsync();
        var facts = new List<BsonDocument>();
        foreach (var name in await databases.ToListAsync())
        {
            var database = mongo.GetDatabase(name);
            using var collections = await database.ListCollectionNamesAsync();
            foreach (var collection in (await collections.ToListAsync()).Where(name => !name.StartsWith("system.", StringComparison.Ordinal)))
            {
                var documents = await database.GetCollection<BsonDocument>(collection).Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
                facts.AddRange(documents.Where(document => document.Names.Any(name => name.Equals("content", StringComparison.OrdinalIgnoreCase)) && IsClientFact(document)));
            }
        }

        return facts;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Host is not null)
        {
            await Host.DisposeAsync();
        }

        await _kernel.DisposeAsync();
        await _mongo.DisposeAsync();
        await _network.DisposeAsync();
        _secrets.Dispose();
    }

    static bool IsClientFact(BsonDocument document)
    {
        var json = document.ToJson();
        return new[] { typeof(ClientRegistered), typeof(ClientRedirectUrisChanged), typeof(ClientSecretRotated), typeof(ClientRetired) }
            .Select(type => ((Cratis.Chronicle.Events.EventTypeAttribute)Attribute.GetCustomAttribute(type, typeof(Cratis.Chronicle.Events.EventTypeAttribute))!).Id.Value)
            .Any(id => json.Contains(id, StringComparison.OrdinalIgnoreCase));
    }
}

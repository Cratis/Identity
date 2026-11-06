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
#pragma warning disable MA0039, CA5400 // Test-only: trust the pinned development image's self-signed health certificate.
    readonly HttpClientHandler _healthHandler = new() { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator };
#pragma warning restore MA0039, CA5400

    public ClientRegistrationFixture()
    {
        _mongo = new MongoDbBuilder("mongo:8.0.14")
            .WithUsername("spec")
            .WithPassword("spec-password")
            .WithReplicaSet("rs0")
            .WithNetwork(_network)
            .WithNetworkAliases("client-registration-mongo")
            .Build();
        _kernel = new ContainerBuilder("cratis/chronicle:19.111.0-development")
            .WithNetwork(_network)
            .WithEnvironment("Cratis__Chronicle__Storage__Type", "MongoDB")
            .WithEnvironment("Cratis__Chronicle__Storage__ConnectionDetails", "mongodb://spec:spec-password@client-registration-mongo:27017/?authSource=admin&replicaSet=rs0&directConnection=true")
            .WithPortBinding(35000, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(35000).ForPath("/health").UsingTls().UsingHttpMessageHandler(_healthHandler)))
            .Build();
        _secrets.WriteKey("signing", "clients", DateTimeOffset.UtcNow.AddHours(-1));
        _secrets.WriteKey("encryption", "clients", DateTimeOffset.UtcNow.AddHours(-1));
        _secrets.WriteDataProtectionKey();
    }

    public ClientRegistrationIdentityHost Host { get; private set; } = null!;

    /// <summary>Gets the started Mongo endpoint shared with the conformance host.</summary>
    public string MongoConnectionString => _mongo.GetConnectionString();

    /// <summary>Gets the started pinned Chronicle endpoint.</summary>
    public string ChronicleConnectionString => $"chronicle://{_kernel.Hostname}:{_kernel.GetMappedPublicPort(35000)}?skipTlsValidation=true";

    public IMongoDatabase Database => new MongoClient(MongoConnectionString).GetDatabase(_database);

    public IMongoCollection<OpenIddictMongoDbApplication> Applications => Database.GetCollection<OpenIddictMongoDbApplication>("openiddict.applications");

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        await StartStores();
        await Restart();
    }

    /// <summary>Starts the shared Mongo and pinned Chronicle pair without creating an Identity host.</summary>
    /// <returns>The startup task.</returns>
    public async Task StartStores()
    {
        try
        {
            await _network.CreateAsync();
            await _mongo.StartAsync();
            await _kernel.StartAsync();
        }
        catch
        {
            await WriteKernelLogs("Chronicle startup failed");
            throw;
        }
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
            ["Identity:Chronicle:ConnectionString"] = ChronicleConnectionString,
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

        try
        {
            Host = new(settings);
            _ = Host.Server;
        }
        catch
        {
            await WriteKernelLogs("Client registration host startup failed");
            throw;
        }
    }

    /// <summary>Reads actual stored event documents, not generated schemas or reconstructed CLR payloads.</summary>
    /// <returns>The persisted client facts across Chronicle databases.</returns>
    public async Task<IReadOnlyList<BsonDocument>> ClientFacts()
    {
        var mongo = new MongoClient(_mongo.GetConnectionString());
        using var databases = await mongo.ListDatabaseNamesAsync();
        var facts = new List<BsonDocument>();
        foreach (var name in (await databases.ToListAsync()).Where(name => name is not ("admin" or "config" or "local")))
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

    /// <summary>Writes bounded kernel output from fixed test data without hiding the original failure.</summary>
    /// <param name="reason">The failure that prompted diagnostics.</param>
    /// <returns>The diagnostic task.</returns>
    public async ValueTask WriteKernelLogs(string reason)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var (stdout, stderr) = await _kernel.GetLogsAsync(ct: timeout.Token);
            await Console.Out.WriteLineAsync($"Chronicle container output: {reason}");
            await Console.Out.WriteLineAsync($"--- stdout (last 300 lines) ---\n{Tail(stdout)}");
            await Console.Out.WriteLineAsync($"--- stderr (last 300 lines) ---\n{Tail(stderr)}");
        }
        catch (Exception error)
        {
            await Console.Out.WriteLineAsync($"Could not read Chronicle container output: {error.Message}");
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (TestContext.Current.TestState?.Result == TestResult.Failed)
        {
            await WriteKernelLogs("Client registration scenario failed");
        }

        if (Host is not null)
        {
            await Host.DisposeAsync();
        }

        await _kernel.DisposeAsync();
        await _mongo.DisposeAsync();
        await _network.DisposeAsync();
        _secrets.Dispose();
        _healthHandler.Dispose();
    }

    static string Tail(string output) => string.Join('\n', output.Split('\n').TakeLast(300));

    static bool IsClientFact(BsonDocument document)
    {
        var json = document.ToJson();
        return new[] { typeof(ClientRegistered), typeof(ClientRedirectUrisChanged), typeof(ClientSecretRotated), typeof(ClientRetired) }
            .Select(type => ((Cratis.Chronicle.Events.EventTypeAttribute)Attribute.GetCustomAttribute(type, typeof(Cratis.Chronicle.Events.EventTypeAttribute))!).Id.Value)
            .Any(id => json.Contains(id, StringComparison.OrdinalIgnoreCase));
    }
}

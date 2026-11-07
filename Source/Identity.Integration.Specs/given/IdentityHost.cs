// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Arc.Commands;
using Cratis.Arc.Tenancy;
using Cratis.Chronicle;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Registrations;
using Cratis.Identity.Accounts;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Protocol;
using Cratis.Identity.Realm;
using Cratis.Identity.Tenants;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Cratis.Identity.Integration.given;

/// <summary>
/// A running Identity host against a real MongoDB, with upstream login replaced by <see cref="FakeUpstream"/>.
/// </summary>
/// <remarks>
/// Everything else is the production pipeline: OpenIddict, the MongoDB stores, the consent page, antiforgery and
/// the token endpoint. The configuration preserves both shipped Direct resources and adds Studio and Stage so resource
/// binding can be exercised, tenants the default user is a member of, and a confidential introspection client.
/// </remarks>
public class IdentityHost : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Issuer = "https://localhost/";
    public const string DirectMcp = "https://cratis.direct/mcp";
    public const string DirectV1 = "https://cratis.direct/v1";
    public const string StudioMcp = "https://cratis.studio/mcp";
    public const string IntrospectionClientId = ResourceSettings.IntrospectionClientId;
    public const string IntrospectionClientSecret = ResourceSettings.IntrospectionClientSecret;
    public const string MemberTenant = "cratis";
    public const string OtherMemberTenant = "second";

    /// <summary>
    /// The MongoDB container. Pinned to 8.0.14: later 8.0 patch releases refuse to start on Linux kernels 6.19+
    /// (SERVER-121912), which is what Docker Desktop currently ships.
    /// </summary>
    readonly MongoDbContainer _mongo = new MongoDbBuilder("mongo:8.0.14").WithReplicaSet().Build();
    readonly string _database = $"identity-specs-{Guid.NewGuid():N}";
    readonly MongoDbContainer _fenceMongo = new MongoDbBuilder("mongo:8.0.14").WithReplicaSet().Build();
    readonly string _fenceDatabase = $"identity-fence-log-{Guid.NewGuid():N}";
    readonly string _fenceInstance = Guid.NewGuid().ToString("N");
    readonly ChronicleKernel _chronicle = new();

    /// <summary>
    /// Gets the host's MongoDB database, for specs that must change what is stored behind Identity's back.
    /// </summary>
    public IMongoDatabase Database => new MongoClient(_mongo.GetConnectionString()).GetDatabase(_database);

    /// <summary>
    /// Gets the connection string of the host's MongoDB container, for specs that need their own databases on it.
    /// </summary>
    public string MongoConnectionString => _mongo.GetConnectionString();

    /// <summary>
    /// Gets the test-only pause points in the refresh-token pipeline.
    /// </summary>
    public RefreshGates RefreshGates { get; } = new();

    /// <summary>Gets the async-flow-local gate for native account projection progress.</summary>
    public ProjectionProgressGate ProjectionProgress { get; } = new();

    /// <summary>
    /// Gets the product callback double, independently mutable per GitHub user.
    /// </summary>
    public TenantsCallbackStub TenantsCallback { get; } = new();

    /// <summary>Gets the entry counters for the real native sign-in pipeline.</summary>
    public NativeCommandProbe NativeCommands { get; } = new();

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        await _mongo.StartAsync();
        await _fenceMongo.StartAsync();
        using var fenceClient = new MongoClient(_fenceMongo.GetConnectionString());
        await fenceClient.GetDatabase(_fenceDatabase).GetCollection<BsonDocument>("fence-custody").InsertOneAsync(new BsonDocument
        {
            { "_id", "metadata" }, { "instanceId", _fenceInstance }, { "allocated", 0L }, { "epoch", 0L }
        });
        await _chronicle.Start();

        // Start the native client and reconcile all artifacts before any spec runs.
        _ = Server;
        await using var scope = Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
        var registration = await store.WaitForRegistration(TimeSpan.FromSeconds(30));
        registration.IsSuccess.ShouldBeTrue();
        registration.Artifacts.ShouldNotBeEmpty();
        await store.Reactors.WaitTillSubscribed<AccountPublications>(TimeSpan.FromSeconds(30));
        await store.Reducers.WaitTillSubscribed<AccountLinkStateReducer>(TimeSpan.FromSeconds(30));
    }

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _chronicle.DisposeAsync();
        await _mongo.DisposeAsync();
        await _fenceMongo.DisposeAsync();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _chronicle.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _mongo.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _fenceMongo.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    /// <summary>Stops the real kernel for a bounded outage scenario.</summary>
    /// <returns>The stopped kernel.</returns>
    public Task StopChronicle() => _chronicle.Stop();

    /// <summary>Restarts the same kernel after an outage scenario without replacing its state.</summary>
    /// <returns>The started kernel.</returns>
    public Task RestartChronicle() => _chronicle.Start();

    /// <summary>
    /// Creates a browser: a client that keeps cookies and surfaces redirects instead of following them.
    /// </summary>
    /// <param name="gitHubUser">The GitHub user the fake upstream login signs in, as <c language="text">id:login</c>.</param>
    /// <returns>The <see cref="Browser"/>.</returns>
    public Browser CreateBrowser(string gitHubUser = FakeUpstream.DefaultUser)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri(Issuer)
        });
        client.DefaultRequestHeaders.Add(FakeUpstream.UserHeader, gitHubUser);
        return new Browser(client);
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // AddCratisIdentity reads the secret store from builder.Configuration while the services are registered, which is before
        // ConfigureAppConfiguration applies. Host settings are visible then, so every setting is also given as one.
        foreach (var (key, value) in Settings().Where(_ => _.Value is not null))
        {
            builder.UseSetting(key, value);
        }

        builder
            .UseEnvironment("Development")
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(Settings()))
            .ConfigureLogging(logging => logging
                .AddFilter("Cratis.Identity", LogLevel.Debug)
                .AddFilter("OpenIddict", LogLevel.Trace))
            .ConfigureTestServices(services =>
            {
                services.Replace(ServiceDescriptor.Singleton<IEventStoreNamespaceResolver>(provider =>
                    new IdentityRealmNamespaceResolver(new SuppliedTenantNamespace(provider.GetRequiredService<ITenantIdAccessor>()))));
                services.AddSingleton(NativeCommands);
                services.AddSingleton<NativeSignInInterleaving>();
                services.AddSingleton<ProtocolCommands>();
                services.Replace(ServiceDescriptor.Singleton<IProtocolCommands, ObservedProtocolCommands>());
                services.AddSingleton<CommandHandlerProviders>();
                services.Replace(ServiceDescriptor.Singleton<ICommandHandlerProviders, ObservedCommandHandlers>());
                services.AddSingleton<CommandHandlerArgumentResolver>();
                services.Replace(ServiceDescriptor.Singleton<ICommandHandlerArgumentResolver, ObservedCommandArguments>());
                services.AddSingleton(ProjectionProgress);
                services.AddScoped<ObserverProgress>();
                services.Replace(ServiceDescriptor.Scoped<IObserverProgress, GatedObserverProgress>());
                services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, FakeUpstream>(FakeUpstream.Scheme, _ => { });
                services.Configure<UpstreamLoginOptions>(_ => _.Scheme = FakeUpstream.Scheme);
                services.AddHttpClient(TenantsCallbackHttpClient.Name).ConfigurePrimaryHttpMessageHandler(() => TenantsCallback);
                services.AddOpenIddict().AddServer(server => server
                    .AddEventHandler(RefreshGates.BarrierDescriptor)
                    .AddEventHandler(RefreshGates.BeforeRedemptionHoldDescriptor)
                    .AddEventHandler(RefreshGates.HoldDescriptor));
            });
    }

    /// <summary>
    /// Gets the in-memory configuration of the host.
    /// </summary>
    /// <returns>The settings.</returns>
    protected virtual Dictionary<string, string?> Settings() => new(ResourceSettings.Create(MemberTenant, OtherMemberTenant, FakeUpstream.DefaultUserId.ToString(CultureInfo.InvariantCulture)))
    {
        ["Identity:Issuer"] = Issuer,
        ["Identity:Accounts:LookupKey"] = Convert.ToBase64String(Enumerable.Range(0, 32).Select(value => (byte)value).ToArray()),
        ["Identity:Accounts:ReadModelCatchUpTimeout"] = "00:00:10",
        ["Identity:Accounts:ConfirmNoLegacyMongoStore"] = "true",
        ["ConnectionStrings:chronicle"] = _chronicle.ConnectionString,
        ["Identity:MongoDB:ConnectionString"] = _mongo.GetConnectionString(),
        ["Identity:MongoDB:Database"] = _database,
        ["Identity:FenceLog:ConnectionString"] = _fenceMongo.GetConnectionString(),
        ["Identity:FenceLog:Database"] = _fenceDatabase,
        ["Identity:FenceLog:InstanceId"] = _fenceInstance,
        ["Identity:FenceLog:MinimumSequence"] = "0",
        ["Identity:GitHub:ClientId"] = "unused-in-specs",
        ["Identity:GitHub:ClientSecret"] = "unused-in-specs"
    };
}

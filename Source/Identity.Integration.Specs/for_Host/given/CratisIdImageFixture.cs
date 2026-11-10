// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using MongoDB.Bson;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Cratis.Identity.Integration.for_Host.given;

/// <summary>
/// Provisions everything a hosted-shared Cratis ID image needs to start: an operational MongoDB replica set, a separate
/// fence-log MongoDB, a Chronicle kernel, and a mounted secret store. Every key and secret is generated at run time and
/// never leaves the temporary directory or the container environment.
/// </summary>
public sealed class CratisIdImageFixture : IAsyncDisposable
{
    const string MongoImage = "mongo:8.0.14";
    const string OperationalAlias = "operational-mongo";
    const string FenceAlias = "fence-mongo";
    const string ChronicleAlias = "chronicle";
    const string FenceInstance = "smoke-fence";
    const int ChroniclePort = 35000;
    const int MongoPort = 27017;
    const int ImagePort = 8080;

    readonly INetwork _network = new NetworkBuilder().Build();
    readonly MongoDbContainer _operational;
    readonly MongoDbContainer _fence;
    readonly IContainer _chronicle;
    readonly SecretStoreDirectory _secrets = new();
    IContainer? _image;

    /// <summary>Initializes a new instance of the <see cref="CratisIdImageFixture"/> class.</summary>
    public CratisIdImageFixture()
    {
        _operational = new MongoDbBuilder(MongoImage).WithReplicaSet().WithNetwork(_network).WithNetworkAliases(OperationalAlias).Build();
        _fence = new MongoDbBuilder(MongoImage).WithReplicaSet().WithNetwork(_network).WithNetworkAliases(FenceAlias).Build();
        _chronicle = new ContainerBuilder(Environment.GetEnvironmentVariable("IDENTITY_CHRONICLE_IMAGE") ?? "cratis/chronicle:19.144.0-development")
            .WithNetwork(_network)
            .WithNetworkAliases(ChronicleAlias)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(ChroniclePort, wait => wait.WithTimeout(TimeSpan.FromMinutes(2))))
            .Build();
    }

    /// <summary>Gets the running image, available after <see cref="StartAsync"/>.</summary>
    /// <exception cref="InvalidOperationException">The fixture has not been started.</exception>
    public IContainer Image => _image ?? throw new InvalidOperationException("The fixture has not been started.");

    /// <summary>Gets the loopback base address of the running image.</summary>
    public Uri BaseAddress => new($"http://{Image.Hostname}:{Image.GetMappedPublicPort(ImagePort)}");

    /// <summary>Starts the stores, the Chronicle kernel and the image, and waits until the image reports readiness.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The startup task.</returns>
    /// <exception cref="InvalidOperationException">The image did not become ready; the message carries the container logs.</exception>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _network.CreateAsync(cancellationToken);
        await Task.WhenAll(_operational.StartAsync(cancellationToken), _fence.StartAsync(cancellationToken), _chronicle.StartAsync(cancellationToken));

        // Independently initialized custody: an empty fence log starts at sequence 0.
        using var fenceClient = new MongoClient(_fence.GetConnectionString());
        await fenceClient.GetDatabase("fence").GetCollection<BsonDocument>("fence-custody").InsertOneAsync(
            new BsonDocument { { "_id", "metadata" }, { "instanceId", FenceInstance }, { "allocated", 0L }, { "epoch", 0L } },
            cancellationToken: cancellationToken);

        _secrets.WriteKey("signing", "a", DateTimeOffset.UtcNow.AddHours(-2));
        _secrets.WriteKey("encryption", "a", DateTimeOffset.UtcNow.AddHours(-2));
        _secrets.WriteDataProtectionKey();

        var builder = new ContainerBuilder(CratisIdImage.Name)
            .WithNetwork(_network)
            .WithResourceMapping(new DirectoryInfo(_secrets.Path), "/secrets")
            .WithPortBinding(ImagePort, true)
            .WithCreateParameterModifier(parameters =>
            {
                foreach (var binding in parameters.HostConfig.PortBindings[$"{ImagePort}/tcp"])
                {
                    binding.HostIP = "127.0.0.1";
                }
            })
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(ImagePort).ForPath("/readyz"), wait => wait.WithTimeout(TimeSpan.FromMinutes(5))));
        foreach (var (name, value) in Settings())
        {
            builder = builder.WithEnvironment(name, value);
        }

        _image = builder.Build();
        try
        {
            await _image.StartAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is TimeoutException or InvalidOperationException)
        {
            var diagnostics = await ReadDiagnosticsAsync();
            throw new InvalidOperationException($"The image did not become ready. Diagnostics follow.{Environment.NewLine}{diagnostics}", exception);
        }
    }

    static string Tail(string text, int lines) => string.Join(Environment.NewLine, text.Split('\n').TakeLast(lines));

    async Task<string> ReadDiagnosticsAsync()
    {
        var builder = new System.Text.StringBuilder();
        foreach (var (name, container) in new[] { ("image", _image), ("chronicle", _chronicle) })
        {
            if (container is null)
            {
                continue;
            }

            try
            {
                var (stdout, stderr) = await container.GetLogsAsync();
                builder
                    .AppendLine($"--- {name} stdout (tail) ---")
                    .AppendLine(Tail(stdout, 150))
                    .AppendLine($"--- {name} stderr (tail) ---")
                    .AppendLine(Tail(stderr, 50));
            }
            catch (Exception exception)
            {
                builder.AppendLine($"--- {name} logs unavailable: {exception.GetType().Name} ---");
            }
        }

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            using var response = await client.GetAsync(new Uri(BaseAddress, "/readyz"));
            builder.AppendLine($"--- /readyz: {(int)response.StatusCode} ---").AppendLine(await response.Content.ReadAsStringAsync());
        }
        catch (Exception exception)
        {
            builder.AppendLine($"--- /readyz probe failed: {exception.GetType().Name} ---");
        }

        return builder.ToString();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_image is not null)
        {
            await _image.DisposeAsync();
        }

        await _chronicle.DisposeAsync();
        await _fence.DisposeAsync();
        await _operational.DisposeAsync();
        await _network.DisposeAsync();
        _secrets.Dispose();
    }

    static string RandomSecret() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();

    static string InNetwork(MongoDbContainer container, string alias)
    {
        var builder = new MongoUrlBuilder(container.GetConnectionString())
        {
            Server = new MongoServerAddress(alias, MongoPort),
            DirectConnection = true
        };
        return builder.ToMongoUrl().ToString();
    }

    Dictionary<string, string> Settings()
    {
        var settings = new Dictionary<string, string>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["Identity__Hosting__Preset"] = "hosted-shared",
            ["Identity__Issuer"] = "https://id.smoke.invalid/",
            ["Identity__SecretStore__Path"] = "/secrets",
            ["Identity__SecurityOwners__0"] = "smoke-owner",
            ["Identity__MongoDB__ConnectionString"] = InNetwork(_operational, OperationalAlias),
            ["Identity__MongoDB__Database"] = "identity-smoke",
            ["Identity__FenceLog__ConnectionString"] = InNetwork(_fence, FenceAlias),
            ["Identity__FenceLog__Database"] = "fence",
            ["Identity__FenceLog__InstanceId"] = FenceInstance,
            ["Identity__FenceLog__MinimumSequence"] = "0",
            ["Identity__Accounts__LookupKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            ["Identity__Accounts__ConfirmNoLegacyMongoStore"] = "true",
            ["Identity__GitHub__ClientId"] = $"smoke-{RandomSecret()}",
            ["Identity__GitHub__ClientSecret"] = RandomSecret(),
            ["Cratis__Chronicle__ConnectionString"] = $"chronicle://{ChronicleAlias}:{ChroniclePort}?skipTlsValidation=true"
        };

        // The shipped catalog needs an authenticated callback for both Direct resources; the optional Studio resource stays disabled.
        foreach (var index in new[] { 0, 1 })
        {
            settings[$"Identity__Resources__{index}__TenantsCallback__Url"] = $"https://direct-{index}.smoke.invalid/tenants";
            settings[$"Identity__Resources__{index}__TenantsCallback__Secret"] = RandomSecret();
        }

        return settings;
    }
}

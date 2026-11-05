// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MongoDb;

namespace Cratis.Identity.Integration.given;

/// <summary>
/// A running Identity host against a real MongoDB, with upstream login replaced by <see cref="FakeUpstream"/>.
/// </summary>
/// <remarks>
/// Everything else is the production pipeline: OpenIddict, the MongoDB stores, the consent page, antiforgery and
/// the token endpoint. The configuration mirrors the shipped one and adds a second resource server so resource
/// binding can be exercised, a tenant the default user is a member of, and a confidential introspection client.
/// </remarks>
public class IdentityHost : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Issuer = "https://localhost/";
    public const string DirectMcp = "https://cratis.direct/mcp";
    public const string DirectV1 = "https://cratis.direct/v1";
    public const string StudioMcp = "https://cratis.studio/mcp";
    public const string IntrospectionClientId = "direct-api";
    public const string IntrospectionClientSecret = "introspection-secret-for-specs";
    public const string MemberTenant = "cratis";
    public const string OtherMemberTenant = "second";

    /// <summary>
    /// The MongoDB container. Pinned to 8.0.14: later 8.0 patch releases refuse to start on Linux kernels 6.19+
    /// (SERVER-121912), which is what Docker Desktop currently ships.
    /// </summary>
    readonly MongoDbContainer _mongo = new MongoDbBuilder("mongo:8.0.14").Build();

    /// <summary>
    /// Gets the test-only pause points in the refresh-token pipeline.
    /// </summary>
    public RefreshGates RefreshGates { get; } = new();

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        await _mongo.StartAsync();

        // Start the host so the client store is reconciled before any spec runs.
        _ = Server;
    }

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _mongo.DisposeAsync();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _mongo.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

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
        builder
            .UseEnvironment("Development")
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(Settings()))
            .ConfigureTestServices(services =>
            {
                services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, FakeUpstream>(FakeUpstream.Scheme, _ => { });
                services.Configure<UpstreamLoginOptions>(_ => _.Scheme = FakeUpstream.Scheme);
                services.AddOpenIddict().AddServer(server => server
                    .AddEventHandler(RefreshGates.BarrierDescriptor)
                    .AddEventHandler(RefreshGates.HoldDescriptor));
            });
    }

    Dictionary<string, string?> Settings() => new()
    {
        ["Identity:Issuer"] = Issuer,
        ["Identity:MongoDB:ConnectionString"] = _mongo.GetConnectionString(),
        ["Identity:MongoDB:Database"] = $"identity-specs-{Guid.NewGuid():N}",
        ["Identity:GitHub:ClientId"] = "unused-in-specs",
        ["Identity:GitHub:ClientSecret"] = "unused-in-specs",
        ["Identity:Resources:0:Tenants:0:Id"] = MemberTenant,
        ["Identity:Resources:0:Tenants:0:Name"] = "Cratis",
        ["Identity:Resources:0:Tenants:0:Members:0"] = FakeUpstream.DefaultUserId.ToString(CultureInfo.InvariantCulture),
        ["Identity:Resources:0:Tenants:1:Id"] = OtherMemberTenant,
        ["Identity:Resources:0:Tenants:1:Name"] = "Second tenant",
        ["Identity:Resources:0:Tenants:1:Members:0"] = FakeUpstream.DefaultUserId.ToString(CultureInfo.InvariantCulture),
        ["Identity:Resources:1:Audience"] = "studio-api",
        ["Identity:Resources:1:DisplayName"] = "Cratis Studio",
        ["Identity:Resources:1:Indicators:0"] = StudioMcp,
        ["Identity:Resources:1:Scopes:0:Name"] = "studio:read",
        ["Identity:Resources:1:Scopes:0:Description"] = "Read Studio",
        ["Identity:Resources:1:Tenants:0:Id"] = "studio",
        ["Identity:Resources:1:Tenants:0:AllowAllAccounts"] = "true",
        ["Identity:IntrospectionClients:0:ClientId"] = IntrospectionClientId,
        ["Identity:IntrospectionClients:0:ClientSecret"] = IntrospectionClientSecret
    };
}

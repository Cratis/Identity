// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Identity.Authorization;
using Cratis.Identity.Clients;
using Cratis.Identity.Clients.MetadataDocuments;
using Cratis.Identity.Hosting;
using Cratis.Identity.Protocol.Stores;
using Cratis.Identity.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_ConfigurationApplicationStore;

public class when_resolving_metadata_through_the_real_manager : Specification
{
    const string Client = "https://client.example/cimd.json";

    [Fact]
    async Task should_let_a_fixed_document_recover_without_a_manager_negative_cache()
    {
        var fetcher = Substitute.For<IMetadataDocumentFetcher>();
        fetcher.FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>()).Returns(MetadataDocumentResult.Refused(MetadataRefusal.Metadata), Accepted());
        await using var provider = Build(fetcher);
        var manager = provider.GetRequiredService<IOpenIddictApplicationManager>();
        (await manager.FindByClientIdAsync(Client, CancellationToken.None)).ShouldBeNull();
        (await manager.FindByClientIdAsync(Client, CancellationToken.None)).ShouldNotBeNull();
        await fetcher.Received(2).FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    async Task should_replace_a_cached_revision_and_refuse_a_failed_revalidation()
    {
        var clock = new Clock();
        var fetcher = Substitute.For<IMetadataDocumentFetcher>();
        fetcher.FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>()).Returns(Accepted(name: "First"), Accepted(name: "Second"), MetadataDocumentResult.Refused(MetadataRefusal.Transport));
        await using var provider = Build(fetcher, clock);
        var manager = provider.GetRequiredService<IOpenIddictApplicationManager>();
        var first = await manager.FindByClientIdAsync(Client, CancellationToken.None);
        var id = await manager.GetIdAsync(first, CancellationToken.None);
        (await manager.FindByIdAsync(id, CancellationToken.None)).ShouldNotBeNull();
        clock.Advance();
        var second = await manager.FindByClientIdAsync(Client, CancellationToken.None);
        (await manager.GetDisplayNameAsync(second, CancellationToken.None)).ShouldEqual("Second");
        (await manager.GetIdAsync(second, CancellationToken.None)).ShouldEqual(id);
        clock.Advance();
        (await manager.FindByIdAsync(id, CancellationToken.None)).ShouldBeNull();
        (await manager.FindByClientIdAsync(Client, CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    async Task should_reestablish_the_same_application_id_in_a_fresh_provider_without_a_reverse_map()
    {
        var fetcher = Substitute.For<IMetadataDocumentFetcher>();
        fetcher.FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>()).Returns(Accepted(refresh: true));
        string retainedApplicationId;
        await using (var original = Build(fetcher))
        {
            var manager = original.GetRequiredService<IOpenIddictApplicationManager>();
            var application = await manager.FindByClientIdAsync(Client, CancellationToken.None);
            retainedApplicationId = await manager.GetIdAsync(application, CancellationToken.None);
        }

        await using var restarted = Build(fetcher);
        var freshManager = restarted.GetRequiredService<IOpenIddictApplicationManager>();
        (await freshManager.FindByIdAsync(retainedApplicationId, CancellationToken.None)).ShouldBeNull();
        var revalidated = await freshManager.FindByClientIdAsync(Client, CancellationToken.None);
        (await freshManager.GetIdAsync(revalidated, CancellationToken.None)).ShouldEqual(retainedApplicationId);
        (await freshManager.GetPermissionsAsync(revalidated, CancellationToken.None)).ShouldContain(Permissions.GrantTypes.RefreshToken);
        (await freshManager.FindByIdAsync(retainedApplicationId, CancellationToken.None)).ShouldNotBeNull();
    }

    [Theory]
    [InlineData("http://127.0.0.1/callback", "http://127.0.0.1:49152/callback", true)]
    [InlineData("http://[::1]/callback", "http://[::1]:49152/callback", true)]
    [InlineData("http://127.0.0.1/callback", "http://127.0.0.1:49152/other", false)]
    [InlineData("http://127.0.0.1/callback", "http://127.0.0.2:49152/callback", false)]
    [InlineData("http://127.0.0.1/callback", "http://localhost:49152/callback", false)]
    [InlineData("http://127.0.0.1/callback", "https://127.0.0.1:49152/callback", false)]
    [InlineData("http://127.0.0.1/callback", "http://127.0.0.1:49152/callback?extra=1", false)]
    [InlineData("https://client.example/callback", "https://client.example/callback", true)]
    [InlineData("https://client.example/callback", "https://client.example:49152/callback", false)]
    public async Task should_preserve_the_real_native_and_web_redirect_matcher(string registered, string requested, bool expected)
    {
        var fetcher = Substitute.For<IMetadataDocumentFetcher>();
        var options = new ClientIdMetadataOptions();
        var result = new MetadataDocumentParser(options).Parse(Client, Encoding.UTF8.GetBytes("{\"client_id\":\"" + Client + "\",\"redirect_uris\":[\"" + registered + "\"]}"));
        result.Descriptor.ShouldNotBeNull();
        fetcher.FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>()).Returns(result);
        await using var provider = Build(fetcher);
        var manager = provider.GetRequiredService<IOpenIddictApplicationManager>();
        var application = await manager.FindByClientIdAsync(Client, CancellationToken.None);
        (await manager.ValidateRedirectUriAsync(application, requested, CancellationToken.None)).ShouldEqual(expected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_limit_localhost_matching_to_the_explicit_native_policy(bool allowed)
    {
        var options = new ClientIdMetadataOptions { AllowLocalhostLoopback = allowed };
        var result = new MetadataDocumentParser(options).Parse(Client, Encoding.UTF8.GetBytes("{\"client_id\":\"" + Client + "\",\"redirect_uris\":[\"http://localhost/callback\"]}"));
        var fetcher = Substitute.For<IMetadataDocumentFetcher>();
        fetcher.FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>()).Returns(result);
        await using var provider = Build(fetcher);
        var manager = provider.GetRequiredService<IOpenIddictApplicationManager>();
        var application = await manager.FindByClientIdAsync(Client, CancellationToken.None);
        if (allowed)
        {
            (await manager.ValidateRedirectUriAsync(application, "http://localhost:49152/callback", CancellationToken.None)).ShouldBeTrue();
            (await manager.ValidateRedirectUriAsync(application, "http://127.0.0.1:49152/callback", CancellationToken.None)).ShouldBeFalse();
        }
        else
        {
            application.ShouldBeNull();
        }
    }

    [Fact]
    async Task should_keep_configured_clients_authoritative_and_ignore_unknown_operational_ids()
    {
        var fetcher = Substitute.For<IMetadataDocumentFetcher>();
        var configured = new ClientApplication(Accepted(name: "Configured").Descriptor!);
        await using var provider = Build(fetcher, configured: configured);
        var manager = provider.GetRequiredService<IOpenIddictApplicationManager>();
        (await manager.GetDisplayNameAsync((await manager.FindByClientIdAsync(Client, CancellationToken.None))!, CancellationToken.None)).ShouldEqual("Configured");
        (await manager.FindByIdAsync(configured.Id, CancellationToken.None)).ShouldNotBeNull();
        (await manager.FindByIdAsync("012345678901234567890123", CancellationToken.None)).ShouldBeNull();
        await fetcher.DidNotReceiveWithAnyArgs().FetchAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    async Task should_grant_only_catalog_resource_permissions_not_remote_scope_text()
    {
        var resources = Substitute.For<IResourceCatalog>();
        resources.All.Returns([new ResourceServer("direct-api", "Direct", ["https://direct.example/mcp"], [new ScopeDefinition("direct:read", "Read direct")])]);
        var fetcher = Substitute.For<IMetadataDocumentFetcher>();
        fetcher.FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>()).Returns(Accepted());
        await using var provider = Build(fetcher, resources: resources);
        var manager = provider.GetRequiredService<IOpenIddictApplicationManager>();
        var application = await manager.FindByClientIdAsync(Client, CancellationToken.None);
        var permissions = await manager.GetPermissionsAsync(application, CancellationToken.None);
        permissions.ShouldContain(Permissions.Prefixes.Scope + "direct:read");
        permissions.ShouldContain(Permissions.Prefixes.Resource + "https://direct.example/mcp");
        permissions.ShouldNotContain(Permissions.Prefixes.Scope + "root");
        permissions.ShouldNotContain(Permissions.GrantTypes.RefreshToken);
        permissions.ShouldNotContain(Permissions.Endpoints.Introspection);
        (await manager.GetRequirementsAsync(application, CancellationToken.None)).ShouldContain(Requirements.Features.ProofKeyForCodeExchange);
    }

    [Theory]
    [InlineData("S256", "direct:read", "https://direct.example/mcp", null)]
    [InlineData(null, "direct:read", "https://direct.example/mcp", "invalid_request")]
    [InlineData("plain", "direct:read", "https://direct.example/mcp", "invalid_request")]
    [InlineData("S256", "root", "https://direct.example/mcp", "invalid_scope")]
    [InlineData("S256", "direct:read", "https://unknown.example/mcp", "invalid_target")]
    public async Task should_use_real_authorization_validators_without_replacing_core_handlers(string? method, string scope, string resource, string? error)
    {
        var target = new ResourceServer("direct-api", "Direct", ["https://direct.example/mcp"], [new ScopeDefinition("direct:read", "Read")]);
        var resources = Substitute.For<IResourceCatalog>();
        resources.All.Returns([target]);
        resources.FindByIndicator("https://direct.example/mcp").Returns(target);
        resources.FindByScope("direct:read").Returns([target]);
        var fetcher = Substitute.For<IMetadataDocumentFetcher>();
        fetcher.FetchAsync(Arg.Any<ClientId>(), Arg.Any<CancellationToken>()).Returns(Accepted());
        await using var provider = Build(fetcher, resources: resources, server: true);
        await using var serviceScope = provider.CreateAsyncScope();
        var request = new OpenIddictRequest
        {
            ClientId = Client,
            RedirectUri = "https://client.example/callback",
            ResponseType = "code",
            Scope = scope,
            CodeChallengeMethod = method,
            CodeChallenge = method is null ? null : new string('a', 43)
        };
        request.SetParameter(Parameters.Resource, resource);
        var validation = new OpenIddictServerEvents.ValidateAuthorizationRequestContext(new OpenIddictServerTransaction
        {
            Logger = NullLogger.Instance,
            Options = provider.GetRequiredService<IOptionsMonitor<OpenIddictServerOptions>>().CurrentValue,
            Request = request,
            Response = new OpenIddictResponse(),
            EndpointType = OpenIddictServerEndpointType.Authorization,
            BaseUri = new Uri("https://issuer.example/"),
            RequestUri = new Uri("https://issuer.example/connect/authorize")
        });
        await serviceScope.ServiceProvider.GetRequiredService<IOpenIddictServerDispatcher>().DispatchAsync(validation);
        validation.Error.ShouldEqual(error);
        validation.IsRejected.ShouldEqual(error is not null);
    }

    static ServiceProvider Build(IMetadataDocumentFetcher fetcher, TimeProvider? clock = null, ClientApplication? configured = null, IResourceCatalog? resources = null, bool server = false)
    {
        var catalog = Substitute.For<IProtocolCatalog>();
        catalog.Applications.Returns(configured is null ? [] : [configured]);
        var resolver = new ClientIdMetadataDocuments(fetcher, new ClientIdMetadataOptions { Enabled = true }, resources ?? Substitute.For<IResourceCatalog>(), clock ?? TimeProvider.System, NullLogger<ClientIdMetadataDocuments>.Instance);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(catalog);
        services.AddSingleton<IClientIdMetadataDocuments>(resolver);
        var openIddict = services.AddOpenIddict().AddCore(core => core.UseConfigurationStores(metadataEnabled: true));
        if (server)
        {
            services.AddSingleton<IGrantPolicy>(new GrantPolicy(resources));
            openIddict.AddServer(options => options.SetIssuer(new Uri("https://issuer.example/"))
                .SetAuthorizationEndpointUris("connect/authorize").SetTokenEndpointUris("connect/token")
                .AllowAuthorizationCodeFlow().AllowRefreshTokenFlow().RequireProofKeyForCodeExchange()
                .RegisterScopes("direct:read", "offline_access").RegisterResources("https://direct.example/mcp")
                .AddEphemeralEncryptionKey().AddEphemeralSigningKey()
                .Configure(configuration => configuration.CodeChallengeMethods.Remove("plain"))
                .AddExplicitResourceValidation());
        }

        return services.BuildServiceProvider();
    }

    static MetadataDocumentResult Accepted(string name = "Client", bool refresh = false) => new(MetadataDescriptor.Create(new ClientIdMetadataDocument(new Uri(Client), name, [new Uri("https://client.example/callback")]) { AllowsRefreshToken = refresh }), MetadataRefusal.None, TimeSpan.FromMinutes(1));

    sealed class Clock : TimeProvider
    {
        DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance() => _now += TimeSpan.FromMinutes(2);
    }
}

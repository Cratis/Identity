// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization;
using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.Clients;
using Cratis.Identity.Clients.MetadataDocuments;
using Cratis.Identity.Configuration;
using Cratis.Identity.ConsentFlow;
using Cratis.Identity.Development;
using Cratis.Identity.Device;
using Cratis.Identity.DPoP;
using Cratis.Identity.Keys;
using Cratis.Identity.Protocol;
using Cratis.Identity.Realm;
using Cratis.Identity.Resources;
using Cratis.Identity.Revocation;
using Cratis.Identity.Storage;
using Cratis.Identity.Tenants;
using Cratis.Identity.Upstream;
using Microsoft.Extensions.Options;
using OpenIddict.MongoDb.Models;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Hosting;

/// <summary>
/// Extension methods for registering Cratis Identity.
/// </summary>
internal static class IdentityServiceCollectionExtensions
{
    static readonly string[] _keySettings = ["SigningCertificate", "EncryptionCertificate"];

    /// <summary>
    /// Adds Cratis Identity: storage, upstream login, the OpenIddict authorization server and the endpoints.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
    /// <param name="environment">The <see cref="IHostEnvironment"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for continuation.</returns>
    /// <exception cref="KeysInConfiguration">Thrown when a signing or encryption certificate is still configured.</exception>
    /// <exception cref="SecretStoreNotConfigured">Thrown outside development without a secret store.</exception>
    /// <exception cref="DevelopmentProfileOnlyInDevelopment">Thrown when the local-development profile is enabled outside Development.</exception>
    /// <exception cref="DevelopmentProfileRefused">Thrown when the local-development profile's configuration or key store is not safe.</exception>
    /// <exception cref="OptionsValidationException">The hosted-shared preset has missing or invalid production settings.</exception>
    /// <exception cref="ChronicleNotConfigured">The native account path has no configured Chronicle client.</exception>
    /// <exception cref="InvalidMongoDBConnectionString">Thrown when the MongoDB client is first resolved with a connection string that cannot be parsed.</exception>
    internal static IServiceCollection AddCratisIdentity(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var section = configuration.GetSection(IdentityOptions.SectionName);
        var startupOptions = section.Get<IdentityOptions>() ?? new IdentityOptions();

        // Keys are custodied by the secret store only. Binding would silently ignore a stale certificate setting.
        foreach (var setting in _keySettings.Where(_ => section.GetSection(_).Exists()))
        {
            throw new KeysInConfiguration($"{IdentityOptions.SectionName}:{setting}");
        }

        // The opt-in local-development profile runs before keys and resources are wired and refuses to run outside Development.
        var development = DevelopmentProfile.Apply(configuration, environment, startupOptions);
        development?.Register(services);

        var hostedValidation = new HostedSharedConfiguration(configuration, environment).Validate(null, startupOptions);
        if (hostedValidation.Failed)
        {
            throw new OptionsValidationException(Options.DefaultName, typeof(IdentityOptions), hostedValidation.Failures);
        }

        // Entirely unconfigured optional resources are disabled once, here, so every consumer and every reload sees the same catalog.
        var disabledAudiences = new HostedSharedConfiguration(configuration, environment).DisabledAudiences(startupOptions);
        HostedSharedConfiguration.Remove(startupOptions, disabledAudiences);

        if (!environment.IsDevelopment() && !startupOptions.SecretStore.IsConfigured)
        {
            throw new SecretStoreNotConfigured();
        }

        if (!services.Any(service => service.ServiceType == typeof(IChronicleClient)))
        {
            throw new ChronicleNotConfigured();
        }

        services.AddOptions<IdentityOptions>().Bind(section).PostConfigure(options => HostedSharedConfiguration.Remove(options, disabledAudiences)).ValidateOnStart();
        if (disabledAudiences.Count > 0)
        {
            services.AddHostedService(provider => new DisabledResourcesNotice(provider.GetRequiredService<ILogger<DisabledResourcesNotice>>(), disabledAudiences));
        }

        services.AddOptions<DeviceGrantOptions>().Bind(section.GetSection("Device"));
        services.AddSingleton<IValidateOptions<IdentityOptions>, IdentityOptionsValidation>();
        services.AddSingleton(TimeProvider.System);

        services.AddRealmNamespaceResolver();

        // Protected-decision commands (sign-in) need the command-aware Chronicle decision reader.
        services.AddCommandAwareDecisionReads();

        // Keep hosted-service startup order: storage guards, key reloading, revocation, account guard, then consent.
        services.AddStorage();
        services.AddSecretStoreKeys(startupOptions, development?.Root);
        services.AddResources();
        services.AddTenants();
        services.AddClients();
        services.AddClientIdMetadataDocuments(startupOptions.Clients.MetadataDocuments.Enabled);
        services.AddAuthorizationEndpoints();
        services.AddDeviceGrant(startupOptions.DeviceGrant.Enabled);
        services.AddRevocation(configuration);
        services.AddProtocolServices();
        services.AddClients(configuration);
        services.AddAccountServices();
        services.AddConsentActivation(configuration.GetSection("Identity:ConsentActivation"));

        services.AddSessionAndUpstreamLogin(environment);
        services.AddIdentityAuthorization();
        services.AddAuthorizationServer(startupOptions, configuration, environment);
        services.AddDPoP(startupOptions.DPoP.Enabled);

        return services;
    }

    static void AddAuthorizationServer(this IServiceCollection services, IdentityOptions startupOptions, IConfiguration configuration, IHostEnvironment environment)
    {
        var endpoints = services.AddIdentityEndpoints(configuration);
        if (!environment.IsDevelopment() && string.IsNullOrWhiteSpace(startupOptions.Issuer))
        {
            throw new IssuerNotConfigured();
        }

        services.AddOpenIddict()
            .AddCore(options => options.UseConfigurationStores(startupOptions.Clients.MetadataDocuments.Enabled)
                .SetDefaultAuthorizationEntity<OpenIddictMongoDbAuthorization>()
                .SetDefaultTokenEntity<OpenIddictMongoDbToken>()
                .ReplaceAuthorizationStore<OpenIddictMongoDbAuthorization, FencedAuthorizationStore>()
                .ReplaceTokenStore<OpenIddictMongoDbToken, FencedTokenStore>())
            .AddServer(options =>
            {
                ConfigureProtocol(options, endpoints.RoutePrefix, startupOptions);
                options.AddExplicitResourceValidation();

                if (startupOptions.DeviceGrant.Enabled)
                {
                    options.AddDeviceHandlers();
                }

                AddEphemeralCredentialsInDevelopment(options, startupOptions);

                var aspNetCore = options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough();

                if (startupOptions.DeviceGrant.Enabled) aspNetCore.EnableEndUserVerificationEndpointPassthrough();

                if (environment.IsDevelopment())
                {
                    aspNetCore.DisableTransportSecurityRequirement();
                }

                options.AddAuthorizationHandlers(startupOptions.SecretStore.IsConfigured)
                    .AddDPoPHandlers(startupOptions.DPoP.Enabled);
            });

        services.AddOptions<OpenIddictServerOptions>().Configure<IOptions<IdentityOptions>, IResourceCatalog>(ConfigureServer);

        if (startupOptions.SecretStore.IsConfigured)
        {
            services.AddSigningKeyOptions();
        }
    }

    static void ConfigureProtocol(OpenIddictServerBuilder server, string routePrefix, IdentityOptions options)
    {
        if (options.Clients.MetadataDocuments.Enabled)
        {
            server.AddEventHandler<OpenIddictServerEvents.HandleConfigurationRequestContext>(builder => builder.UseSingletonHandler<AdvertiseClientIdMetadataDocuments>());
        }

        server.SetAuthorizationEndpointUris($"{routePrefix}/connect/authorize".TrimStart('/'))
            .SetTokenEndpointUris($"{routePrefix}/connect/token".TrimStart('/'))
            .SetRevocationEndpointUris($"{routePrefix}/connect/revocation".TrimStart('/'))
            .SetIntrospectionEndpointUris($"{routePrefix}/connect/introspect".TrimStart('/'))
            .AllowAuthorizationCodeFlow()
            .AllowRefreshTokenFlow()
            .AddTokenExchangeFlow(options)
            .RequireProofKeyForCodeExchange()
            .DisableSlidingRefreshTokenExpiration()
            .SetRefreshTokenReuseLeeway(null)
            .DisableAccessTokenEncryption();

        if (options.DeviceGrant.Enabled)
        {
            server.AllowDeviceAuthorizationFlow()
                .SetDeviceAuthorizationEndpointUris($"{routePrefix}/connect/device".TrimStart('/'))
                .SetEndUserVerificationEndpointUris($"{routePrefix}/device".TrimStart('/'))
                .AddEventHandler<OpenIddictServerEvents.HandleDeviceAuthorizationRequestContext>(builder => builder.UseInlineHandler(context =>
                {
                    context.SkipRequest();
                    return ValueTask.CompletedTask;
                }));
        }
    }

    static void ConfigureServer(OpenIddictServerOptions server, IOptions<IdentityOptions> identity, IResourceCatalog resources)
    {
        var options = identity.Value;
        if (!string.IsNullOrWhiteSpace(options.Issuer))
        {
            server.Issuer = new Uri(options.Issuer, UriKind.Absolute);
        }

        // S256 only; "plain" offers no protection against an intercepted authorization request.
        server.CodeChallengeMethods.Remove(CodeChallengeMethods.Plain);

        server.AccessTokenLifetime = options.Tokens.AccessTokenLifetime;
        server.AuthorizationCodeLifetime = options.Tokens.AuthorizationCodeLifetime;
        server.RefreshTokenLifetime = options.Tokens.RefreshTokenLifetime;
        server.DeviceCodeLifetime = options.DeviceGrant.CodeLifetime;
        server.UserCodeLifetime = options.DeviceGrant.CodeLifetime;
        if (options.DeviceGrant.Enabled)
        {
            server.UserCodeLength = options.DeviceGrant.UserCodeLength;
            server.UserCodeDisplayFormat = options.DeviceGrant.UserCodeLength == 8 ? "{0}{1}{2}{3}-{4}{5}{6}{7}" : null;
            server.UserCodeCharset.Clear();
            server.UserCodeCharset.UnionWith(options.DeviceGrant.UserCodeCharset.Select(_ => _.ToString()));
        }

        // OAuth only: no OpenID Connect sign-in is offered to clients yet, so identity tokens are not advertised.
        server.Scopes.Remove(Scopes.OpenId);
        server.Scopes.UnionWith(ProtocolScopes.All);
        server.Scopes.UnionWith(resources.All.SelectMany(_ => _.Scopes).Select(_ => _.Name));
        server.Resources.UnionWith(resources.All.SelectMany(_ => _.Indicators).Select(_ => new Uri(_, UriKind.Absolute)));
    }

    static void AddEphemeralCredentialsInDevelopment(OpenIddictServerBuilder server, IdentityOptions options)
    {
        if (!options.SecretStore.IsConfigured)
        {
            // Development only: AddCratisIdentity refuses to start outside it without a secret store.
            server.AddEphemeralSigningKey().AddEphemeralEncryptionKey();
        }
    }
}

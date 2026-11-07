// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Identity.Accounts;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Authorization;
using Cratis.Identity.Clients;
using Cratis.Identity.Configuration;
using Cratis.Identity.ConsentFlow;
using Cratis.Identity.ConsentFlow.Operational;
using Cratis.Identity.Device;
using Cratis.Identity.Device.Operational;
using Cratis.Identity.Keys;
using Cratis.Identity.Operational;
using Cratis.Identity.Protocol;
using Cratis.Identity.Realm;
using Cratis.Identity.Resources;
using Cratis.Identity.Revocation;
using Cratis.Identity.Revocation.Operational;
using Cratis.Identity.Storage;
using Cratis.Identity.Tenants;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenIddict.MongoDb;
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

        if (!environment.IsDevelopment() && !startupOptions.SecretStore.IsConfigured)
        {
            throw new SecretStoreNotConfigured();
        }

        if (!services.Any(service => service.ServiceType == typeof(IChronicleClient)))
        {
            throw new ChronicleNotConfigured();
        }

        services.AddOptions<IdentityOptions>().Bind(section).ValidateOnStart();
        services.AddSingleton<IValidateOptions<IdentityOptions>, IdentityOptionsValidation>();
        services.AddSingleton(TimeProvider.System);

        AddRealmNamespaceResolver(services);
        services.AddStorage();
        services.AddSecretStoreKeys(startupOptions);
        services.AddDomain(configuration, startupOptions.DeviceGrant.Enabled);
        services.AddAccountServices();
        services.AddConsentActivation(configuration.GetSection("Identity:ConsentActivation"));

        services.AddSessionAndUpstreamLogin(environment);
        services.AddAuthorizationServer(startupOptions, environment);

        return services;
    }

    /// <summary>Adds native account admission, readers and lookup-key startup protection.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for continuation.</returns>
    internal static IServiceCollection AddAccountServices(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IProtocolCommands, ProtocolCommands>();
        services.AddScoped<IObserverProgress, ObserverProgress>();
        services.AddScoped<ReadModelCatchUp>();
        services.AddSingleton<IUpstreamLookupKeys, ConfiguredUpstreamLookupKeys>();
        services.AddSingleton<UpstreamKeyDigester>();
        services.AddSingleton<VerifiedKeyGate>();
        services.AddScoped<AccountStore>();
        services.AddScoped<UpstreamLookups>();
        services.AddScoped<AccountReader>();
        services.AddScoped<IAccountReader>(provider => provider.GetRequiredService<AccountReader>());
        services.AddScoped<EventSourcedAccountAdapter>();
        services.AddScoped<IAccountSignIn>(provider => provider.GetRequiredService<EventSourcedAccountAdapter>());
        services.AddScoped<VerifiedUpstreamSignIn>();
        services.AddScoped<IVerifiedUpstreamSignIn>(provider => provider.GetRequiredService<VerifiedUpstreamSignIn>());
        services.AddScoped<UpstreamKeyLinking>();
        services.AddLookupRevisionGuard();
        return services;
    }

    /// <summary>
    /// Adds the Identity session cookie, antiforgery and the upstream login.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="environment">The <see cref="IHostEnvironment"/>.</param>
    internal static void AddSessionAndUpstreamLogin(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddOptions<UpstreamLoginOptions>();
        services.AddAntiforgery(options =>
        {
            options.FormFieldName = ConsentForm.AntiforgeryField;
            options.Cookie.Name = "cratis-identity-antiforgery";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        });

        services
            .AddAuthentication(IdentitySchemes.Session)
            .AddCookie(IdentitySchemes.Session, options =>
            {
                options.Cookie.Name = "cratis-identity-session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
                options.SlidingExpiration = false;
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
            })
            .AddGitHubUpstream();

        services.AddOptions<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>(IdentitySchemes.Session)
            .Configure<IOptions<IdentityOptions>>((cookie, identity) => cookie.ExpireTimeSpan = identity.Value.SessionLifetime);
    }

    /// <summary>Adds the dormant, fail-closed consent state machine; the live endpoint is intentionally unchanged.</summary>
    /// <param name="services">The services.</param>
    /// <param name="configuration">The consent activation options.</param>
    internal static void AddConsentActivation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ConsentActivationOptions>().Bind(configuration).ValidateDataAnnotations().ValidateOnStart();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<IConsentRequestStore, MongoConsentRequestStore>();
        services.TryAddScoped<IConsentEvidence, UnavailableConsentServices>();
        services.TryAddScoped<IRevocationFence, UnavailableConsentServices>();
        services.TryAddScoped<IAuthorizationActivator, UnavailableConsentServices>();
        services.TryAddScoped<ConsentActivation>();
        services.AddHostedService<ConsentReconciler>();
    }

    static void AddRealmNamespaceResolver(IServiceCollection services)
    {
        var previous = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(IEventStoreNamespaceResolver));
        if (previous is null)
        {
            return;
        }

        services.Remove(previous);
        services.AddSingleton<IEventStoreNamespaceResolver>(provider => new IdentityRealmNamespaceResolver(
            (IEventStoreNamespaceResolver)(previous.ImplementationInstance ?? previous.ImplementationFactory?.Invoke(provider) ??
                ActivatorUtilities.CreateInstance(provider, previous.ImplementationType!))));
    }

    static void AddStorage(this IServiceCollection services)
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

    static void AddSecretStoreKeys(this IServiceCollection services, IdentityOptions startupOptions)
    {
        var dataProtection = services.AddDataProtection().SetApplicationName("Cratis.Identity");
        if (!startupOptions.SecretStore.IsConfigured)
        {
            // Development only: ephemeral realm keys and the default Data Protection ring.
            return;
        }

        // The secret store is the custody of the ring, so keys are neither generated nor wrapped by Identity.
        dataProtection.DisableAutomaticKeyGeneration();
        services.AddSingleton(sp =>
        {
            var keys = new SecretStoreKeys(
                sp.GetRequiredService<IOptions<IdentityOptions>>().Value.SecretStore,
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<ILogger<SecretStoreKeys>>());

            // The first read validates the store, so an invalid one stops the host when the service is first resolved.
            _ = keys.Current;
            return keys;
        });
        services.AddSecretStoreKeyRing();
        services.AddHostedService<KeyRingReloading>();
    }

    static void AddDomain(this IServiceCollection services, IConfiguration configuration, bool deviceEnabled)
    {
        services.AddSingleton<IResourceCatalog, ResourceCatalog>();
        services.AddSingleton<IGrantPolicy, GrantPolicy>();
        services.AddSingleton<ConfiguredTenantDirectory>();
        services.AddSingleton<ProductTenantsCallback>();
        services.AddSingleton<ITenantDirectory, TenantDirectory>();
        services.AddHttpClient(TenantsCallbackHttpClient.Name, client => client.Timeout = TimeSpan.FromSeconds(10))
            .ConfigurePrimaryHttpMessageHandler(TenantsCallbackHttpClient.CreateHandler)
            .RemoveAllLoggers();
        services.AddSingleton<ClientDescriptors>();
        services.AddScoped<SignedInAccounts>();
        services.AddScoped<ConsentPages>();
        services.AddScoped<AuthorizationEndpoint>();
        services.AddScoped<TokenEndpoint>();
        services.AddHttpContextAccessor();

        // TokenEndpoint shares this admission helper; device handlers and state remain opt-in.
        services.AddScoped<DeviceGrantAdmission>();
        if (deviceEnabled)
        {
            services.AddSingleton<IDeviceGrantStateStore, MongoDeviceGrantStateStore>();
            services.AddScoped<DeviceGrantRateLimiter>();
            services.AddScoped<DeviceAuthorizationEndpoint>();
            services.AddScoped<DeviceVerificationEndpoint>();
            services.AddSingleton<DeviceGrantApproval>();
        }
        services.AddSingleton(configuration.GetSection("Identity:FenceLog").Get<FenceLogOptions>() ?? new FenceLogOptions());
        services.AddSingleton<IndependentFenceLogConnection>();
        services.AddSingleton(sp => new MongoIndependentFenceLog(() => sp.GetRequiredService<IndependentFenceLogConnection>().Database, sp.GetRequiredService<TimeProvider>(), () => sp.GetRequiredService<RecoverySession>().Verified));
        services.AddSingleton<IIndependentFenceLog>(sp => sp.GetRequiredService<MongoIndependentFenceLog>());
        services.AddSingleton<IRecoveryLog>(sp => sp.GetRequiredService<MongoIndependentFenceLog>());
        services.AddSingleton<IReservationLog>(sp => sp.GetRequiredService<MongoIndependentFenceLog>());
        services.AddSingleton<MongoFenceStore>();
        services.TryAddSingleton<IFenceStore>(sp => sp.GetRequiredService<MongoFenceStore>());
        services.AddSingleton<IReservationAuthority>(sp => sp.GetRequiredService<MongoFenceStore>());
        services.AddSingleton<IRecoveryStore, MongoFenceRecovery>();
        services.AddSingleton<RecoveryGate>();
        services.AddSingleton<RecoverySession>();
        services.AddScoped<ReconcileReservation>();
        services.AddScoped<ReconcileRecovery>();
        services.AddScoped<IRecoveryFactWriter, ChronicleRecoveryFactWriter>();
        services.TryAddSingleton<IFenceLog, IndependentFenceLogAppender>();
        services.AddHealthChecks().AddCheck<RecoveryReadiness>("identity-restriction-recovery");
        services.AddScoped<ConfirmFenceLog>();
        services.TryAddScoped<IRevocationFactWriter, ChronicleRevocationFactWriter>();
        services.AddScoped<DispatchRevocationOutbox>();
        services.AddHostedService<RevocationOutboxWorker>();
        services.AddScoped<ReadRefreshGrant>();
        services.AddScoped<RefreshIssuance>();
        services.AddScoped<GrantRevocation>();
    }

    static void AddAuthorizationServer(this IServiceCollection services, IdentityOptions startupOptions, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment() && string.IsNullOrWhiteSpace(startupOptions.Issuer))
        {
            throw new IssuerNotConfigured();
        }

        services.AddOpenIddict()
            .AddCore(options => options.UseConfigurationStores()
                .SetDefaultAuthorizationEntity<OpenIddictMongoDbAuthorization>()
                .SetDefaultTokenEntity<OpenIddictMongoDbToken>()
                .ReplaceAuthorizationStore<OpenIddictMongoDbAuthorization, FencedAuthorizationStore>()
                .ReplaceTokenStore<OpenIddictMongoDbToken, FencedTokenStore>())
            .AddServer(options =>
            {
                options
                    .SetAuthorizationEndpointUris("connect/authorize")
                    .SetTokenEndpointUris("connect/token")
                    .SetRevocationEndpointUris("connect/revocation")
                    .SetIntrospectionEndpointUris("connect/introspect")
                    .AllowAuthorizationCodeFlow()
                    .AllowRefreshTokenFlow()
                    .RequireProofKeyForCodeExchange()
                    .DisableSlidingRefreshTokenExpiration()
                    .SetRefreshTokenReuseLeeway(null)
                    .DisableAccessTokenEncryption();

                if (startupOptions.DeviceGrant.Enabled)
                {
                    options.AllowDeviceAuthorizationFlow()
                        .SetDeviceAuthorizationEndpointUris("connect/device")
                        .SetEndUserVerificationEndpointUris("device")
                        .AddEventHandler<OpenIddictServerEvents.HandleDeviceAuthorizationRequestContext>(builder => builder.UseInlineHandler(context =>
                        {
                            context.SkipRequest();
                            return ValueTask.CompletedTask;
                        }));
                }

                if (startupOptions.DeviceGrant.Enabled)
                {
                    options.RemoveEventHandler(OpenIddictServerHandlers.AttachAuthorization.Descriptor)
                        .AddEventHandler(AttachDeviceAuthorization.Descriptor)
                        .RemoveEventHandler(OpenIddictServerHandlers.RedeemTokenEntry.Descriptor)
                        .AddEventHandler(RedeemDeviceCodeOnce.Descriptor)
                        .AddEventHandler(BindDeviceAuthorization.Descriptor)
                        .AddEventHandler(DeviceVerificationGuard.Descriptor)
                        .AddEventHandler(DevicePollingAdmissionGuard.Descriptor)
                        .AddEventHandler(DevicePollingGuard.Descriptor)
                        .AddEventHandler(DeviceAuthorizationResponse.Descriptor)
                        .AddEventHandler(DeviceVerificationResponse.Descriptor)
                        .AddEventHandler(GuardDeviceIssuance.BeforeGeneration)
                        .AddEventHandler(GuardDeviceIssuance.BeforeResponse);
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

                if (startupOptions.SecretStore.IsConfigured)
                {
                    options.AddEventHandler(RefuseIssuanceWithoutUsableKey.Descriptor);
                }

                options.AddEventHandler(RefuseIssuanceDuringRecovery.Descriptor)
                    .AddEventHandler(RefuseIssuanceDuringRecovery.ValidationDescriptor)
                    .AddEventHandler(RefuseIssuanceDuringRecovery.EntryValidationDescriptor)
                    .AddEventHandler(RefuseIssuanceDuringRecovery.RequestDescriptor)
                    .AddEventHandler(RejectExpiredTokenEntries.Descriptor)
                    .AddEventHandler(RejectFencedGrants.Descriptor)
                    .AddEventHandler(RejectFencedGrants.SignInDescriptor)
                    .AddEventHandler(RevokeGrantOnRefreshTokenReuse.Descriptor)
                    .AddEventHandler(RedeemRefreshTokenOnce.Descriptor)
                    .AddEventHandler(RevokeGrantOnRefreshTokenRevocation.Descriptor);
            });

        services.AddOptions<OpenIddictServerOptions>().Configure<IOptions<IdentityOptions>, IResourceCatalog>(ConfigureServer);

        if (startupOptions.SecretStore.IsConfigured)
        {
            // Registered after AddOpenIddict(), so the key order is set after OpenIddict has sorted its credentials.
            services.AddSingleton<ConfigureServerKeys>();
            services.AddSingleton<IConfigureOptions<OpenIddictServerOptions>>(sp => sp.GetRequiredService<ConfigureServerKeys>());
            services.AddSingleton<IPostConfigureOptions<OpenIddictServerOptions>>(sp => sp.GetRequiredService<ConfigureServerKeys>());
            services.AddSingleton<IOptionsChangeTokenSource<OpenIddictServerOptions>, SecretStoreKeysChangeTokenSource>();
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

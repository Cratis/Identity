// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization;
using Cratis.Identity.Clients;
using Cratis.Identity.Configuration;
using Cratis.Identity.Resources;
using Cratis.Identity.Storage;
using Cratis.Identity.Tenants;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Hosting;

/// <summary>
/// Extension methods for registering Cratis Identity.
/// </summary>
public static class IdentityServiceCollectionExtensions
{
    /// <summary>
    /// Adds Cratis Identity: storage, upstream login, the OpenIddict authorization server and the endpoints.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
    /// <param name="environment">The <see cref="IHostEnvironment"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddCratisIdentity(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var section = configuration.GetSection(IdentityOptions.SectionName);
        var startupOptions = section.Get<IdentityOptions>() ?? new IdentityOptions();

        services.AddOptions<IdentityOptions>().Bind(section).ValidateOnStart();
        services.AddSingleton<IValidateOptions<IdentityOptions>, IdentityOptionsValidation>();
        services.AddSingleton(TimeProvider.System);

        services.AddStorage(startupOptions);
        services.AddDomain();
        services.AddSessionAndUpstreamLogin();
        services.AddAuthorizationServer(startupOptions, environment);

        return services;
    }

    static void AddStorage(this IServiceCollection services, IdentityOptions startupOptions)
    {
        services.AddSingleton<IMongoClient>(sp => new MongoClient(sp.GetRequiredService<IOptions<IdentityOptions>>().Value.MongoDB.ConnectionString));
        services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(sp.GetRequiredService<IOptions<IdentityOptions>>().Value.MongoDB.Database));
        services.AddSingleton<StorageIndexes>();

        var dataProtection = services.AddDataProtection().SetApplicationName("Cratis.Identity");
        if (startupOptions.EncryptionCertificate.IsConfigured)
        {
            // Without a certificate (development) the key ring is stored unencrypted in MongoDB.
            dataProtection.ProtectKeysWithCertificate(Load(startupOptions.EncryptionCertificate));
        }

        services.AddOptions<KeyManagementOptions>().Configure<IMongoDatabase>((options, database) => options.XmlRepository = new MongoXmlRepository(database));

        services.AddHostedService<ClientRegistrations>();
        services.AddHostedService<TokenPruning>();
    }

    static void AddDomain(this IServiceCollection services)
    {
        services.AddSingleton<IAccounts, Accounts.Accounts>();
        services.AddSingleton<IResourceCatalog, ResourceCatalog>();
        services.AddSingleton<IGrantPolicy, GrantPolicy>();
        services.AddSingleton<ITenantDirectory, ConfiguredTenantDirectory>();
        services.AddSingleton<ClientDescriptors>();
        services.AddScoped<SignedInAccounts>();
        services.AddScoped<ConsentPages>();
        services.AddScoped<AuthorizationEndpoint>();
        services.AddScoped<TokenEndpoint>();
    }

    static void AddSessionAndUpstreamLogin(this IServiceCollection services)
    {
        services.AddOptions<UpstreamLoginOptions>();
        services.AddAntiforgery(options =>
        {
            options.FormFieldName = ConsentForm.AntiforgeryField;
            options.Cookie.Name = "cratis-identity-antiforgery";
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });

        services
            .AddAuthentication(IdentitySchemes.Session)
            .AddCookie(IdentitySchemes.Session, options =>
            {
                options.Cookie.Name = "cratis-identity-session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
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

    static void AddAuthorizationServer(this IServiceCollection services, IdentityOptions startupOptions, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment() && string.IsNullOrWhiteSpace(startupOptions.Issuer))
        {
            throw new IssuerNotConfigured();
        }

        services.AddOpenIddict()
            .AddCore(options => options.UseMongoDb())
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

                AddCredentials(options, startupOptions, environment);

                var aspNetCore = options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough();

                if (environment.IsDevelopment())
                {
                    aspNetCore.DisableTransportSecurityRequirement();
                }

                options.AddEventHandler(RevokeGrantOnRefreshTokenRevocation.Descriptor);
            });

        services.AddOptions<OpenIddictServerOptions>().Configure<IOptions<IdentityOptions>, IResourceCatalog>(ConfigureServer);
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

        // OAuth only: no OpenID Connect sign-in is offered to clients yet, so identity tokens are not advertised.
        server.Scopes.Remove(Scopes.OpenId);
        server.Scopes.UnionWith(ProtocolScopes.All);
        server.Scopes.UnionWith(resources.All.SelectMany(_ => _.Scopes).Select(_ => _.Name));
        server.Resources.UnionWith(resources.All.SelectMany(_ => _.Indicators).Select(_ => new Uri(_, UriKind.Absolute)));
    }

    static void AddCredentials(OpenIddictServerBuilder server, IdentityOptions options, IHostEnvironment environment)
    {
        if (options.SigningCertificate.IsConfigured && options.EncryptionCertificate.IsConfigured)
        {
            server
                .AddSigningCertificate(Load(options.SigningCertificate))
                .AddEncryptionCertificate(Load(options.EncryptionCertificate));
            return;
        }

        if (!environment.IsDevelopment())
        {
            throw new CertificatesNotConfigured();
        }

        server.AddEphemeralSigningKey().AddEphemeralEncryptionKey();
    }

    static X509Certificate2 Load(CertificateOptions certificate) =>
        X509CertificateLoader.LoadPkcs12FromFile(certificate.Path, certificate.Password);
}

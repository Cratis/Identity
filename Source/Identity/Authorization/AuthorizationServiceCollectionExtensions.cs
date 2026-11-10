// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.RateLimiting;
using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.Configuration;
using Cratis.Identity.Hosting;
using Cratis.Identity.Operational;
using Microsoft.Extensions.Options;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

/// <summary>Registers authorization policies and grant endpoints.</summary>
internal static class AuthorizationServiceCollectionExtensions
{
    /// <summary>Adds the explicit issuer security owner authorization policy.</summary>
    /// <param name="services">The services.</param>
    /// <returns>The services for continuation.</returns>
    internal static IServiceCollection AddIdentityAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization();
        services.AddOptions<Microsoft.AspNetCore.Authorization.AuthorizationOptions>()
            .Configure<IOptions<IdentityOptions>>((authorization, identity) =>
                authorization.AddPolicy(IdentityPolicies.IssuerSecurityOwner, policy => policy
                    .AddAuthenticationSchemes(IdentitySchemes.Session)
                    .RequireAuthenticatedUser()
                    .RequireAssertion(context => context.User.Identities
                        .Where(_ => _.IsAuthenticated)
                        .SelectMany(_ => _.FindAll(Claims.Subject))
                        .Any(subject => identity.Value.SecurityOwners.Contains(subject.Value, StringComparer.Ordinal)))));
        return services;
    }

    /// <summary>Adds authorization, token exchange and refresh grant services.</summary>
    /// <param name="services">The services.</param>
    internal static void AddAuthorizationEndpoints(this IServiceCollection services)
    {
        services.AddSingleton<IGrantPolicy, GrantPolicy>();
        services.AddScoped<ConsentPages>();
        services.AddScoped<AuthorizationEndpoint>();
        services.AddScoped<TokenEndpoint>();
        services.AddHttpContextAccessor();
        services.AddScoped<TokenExchangeGrant>();
        services.AddSingleton<ISubjectTokenReplayGuard, MongoSubjectTokenReplayGuard>();
        services.AddSingleton<TokenRequestRateLimiter>();
        services.AddSingleton<ISubjectTokenValidator, SubjectTokenValidator>();
        services.AddSingleton<IExternalIssuerKeys, ExternalIssuerKeys>();
        services.AddHttpClient(ExternalIssuerKeys.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(ExternalIssuerKeys.CreateHandler)
            .RemoveAllLoggers();
        services.AddScoped<ReadRefreshGrant>();
        services.AddScoped<RefreshIssuance>();
        services.AddScoped<GrantRevocation>();
    }
}

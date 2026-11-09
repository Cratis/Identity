// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.given;

public static class Configurations
{
    public const string DirectAudience = "direct-api";
    public const string DirectMcp = "https://cratis.direct/mcp";
    public const string DirectV1 = "https://cratis.direct/v1";
    public const string StudioAudience = "studio-api";
    public const string StudioMcp = "https://cratis.studio/mcp";
    public const string MemberGitHubId = "134365";

    public static IdentityOptions TwoResourceServers() => new()
    {
        Accounts = new() { LookupKey = Convert.ToBase64String(new byte[32]) },
        Resources =
        [
            new ResourceServerOptions
            {
                Audience = DirectAudience,
                ReceivesUpstreamIdentity = true,
                DisplayName = "Cratis Direct",
                Indicators = [DirectMcp],
                Scopes =
                [
                    new ScopeOptions { Name = "direct:read", Description = "Read" },
                    new ScopeOptions { Name = "direct:content.write", Description = "Write content" },
                    new ScopeOptions { Name = "direct:work", Description = "Start work" }
                ],
                Tenants =
                [
                    new TenantOptions { Id = "cratis", Name = "Cratis", Members = [MemberGitHubId] },
                    new TenantOptions { Id = "public", Name = "Public", AllowAllAccounts = true },
                    new TenantOptions { Id = "private", Name = "Private", Members = ["999"] }
                ]
            },
            new ResourceServerOptions
            {
                Audience = StudioAudience,
                DisplayName = "Cratis Studio",
                Indicators = [StudioMcp],
                Scopes = [new ScopeOptions { Name = "studio:read", Description = "Read" }],
                Tenants = [new TenantOptions { Id = "studio-tenant", Name = "Studio", AllowAllAccounts = true }]
            }
        ]
    };

    public static IdentityOptions CallbackResources()
    {
        var options = TwoResourceServers();
        foreach (var resource in options.Resources)
        {
            resource.Tenants.Clear();
            resource.TenantsCallback = new TenantsCallbackOptions { Url = "https://products.example/tenants", Secret = new string('s', 32) };
        }

        return options;
    }

    public const string ExchangeClientId = "authproxy";
    public const string ExchangeClientSecret = "exchange-client-secret-0123456789abcdef";
    public const string ExternalIssuer = "https://login.partner.example";
    public const string ExternalAudience = "https://identity.example/exchange";

    public static IdentityOptions WithTokenExchange()
    {
        var options = CallbackResources();
        options.Issuer = "https://identity.example/";
        options.TokenExchange = new()
        {
            Issuers =
            [
                new ExternalIssuerOptions
                {
                    Issuer = ExternalIssuer,
                    JwksUri = $"{ExternalIssuer}/.well-known/jwks.json",
                    Audiences = [ExternalAudience]
                }
            ],
            Clients =
            [
                new TokenExchangeClientOptions
                {
                    ClientId = ExchangeClientId,
                    ClientSecret = ExchangeClientSecret,
                    Issuers = [ExternalIssuer],
                    Resources = [DirectAudience],
                    Scopes = ["direct:read"]
                }
            ]
        };
        return options;
    }

    public static IOptions<IdentityOptions> AsOptions(this IdentityOptions options) => Options.Create(options);

    public static IOptionsMonitor<IdentityOptions> AsMonitor(this IdentityOptions options)
    {
        var monitor = Substitute.For<IOptionsMonitor<IdentityOptions>>();
        monitor.CurrentValue.Returns(options);
        return monitor;
    }
}

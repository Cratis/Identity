// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Integration.given;

/// <summary>
/// An <see cref="IdentityHost"/> with token exchange enabled for one client and one fake external issuer.
/// </summary>
public class ExchangeIdentityHost : IdentityHost
{
    /// <summary>
    /// The name of the restricted JWKS client token exchange fetches issuer keys with.
    /// </summary>
    const string JwksClientName = "Cratis.Identity.TokenExchangeJwks";

    /// <summary>
    /// Gets the fake external issuer.
    /// </summary>
    public ExternalIssuer External { get; } = new();

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services => services
            .AddHttpClient(JwksClientName)
            .ConfigurePrimaryHttpMessageHandler(() => External));
    }

    /// <inheritdoc/>
    protected override Dictionary<string, string?> Settings()
    {
        var settings = base.Settings();
        settings["Identity:TokenExchange:Issuers:0:Issuer"] = External.Issuer;
        settings["Identity:TokenExchange:Issuers:0:JwksUri"] = "https://github.example/.well-known/jwks.json";
        settings["Identity:TokenExchange:Issuers:0:Audiences:0"] = ExternalIssuer.Audience;
        settings["Identity:TokenExchange:Clients:0:ClientId"] = ExternalIssuer.ClientId;
        settings["Identity:TokenExchange:Clients:0:ClientSecret"] = ExternalIssuer.ClientSecret;
        settings["Identity:TokenExchange:Clients:0:Issuers:0"] = External.Issuer;
        settings["Identity:TokenExchange:Clients:0:Resources:0"] = ResourceSettings.IntrospectionClientId;
        settings["Identity:TokenExchange:Clients:0:Scopes:0"] = "direct:mcp:read";
        return settings;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            External.Dispose();
        }
    }
}

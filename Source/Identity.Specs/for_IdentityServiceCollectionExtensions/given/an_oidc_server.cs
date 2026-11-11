// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenIddict.Server;

namespace Cratis.Identity.for_IdentityServiceCollectionExtensions.given;

public class an_oidc_server : Specification
{
    ServiceProvider _provider;
    private protected OpenIddictServerOptions _server;
    protected virtual bool IssuanceEnabled => true;

    void Establish()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Configuration.Sources.Clear();
        var settings = new Dictionary<string, string?>
        {
            ["Identity:Issuer"] = "https://identity.example/",
            ["Identity:AllowEphemeralKeys"] = "true",
            ["Identity:Persistence:DisableTokenStorage"] = "true",
            ["Identity:Persistence:DisableAuthorizationStorage"] = "true",
            ["Identity:Oidc:IssuanceEnabled"] = IssuanceEnabled.ToString(),
            ["Identity:Oidc:ConsentDecision"] = "ImplicitFencedPath",
            ["Identity:Tokens:IdentityTokenLifetime"] = "00:04:00"
        };
        builder.Configuration.AddInMemoryCollection(settings);
        builder.Services.AddSingleton(Substitute.For<IChronicleClient>());
        builder.AddCratisIdentity();
        _provider = builder.Services.BuildServiceProvider();
        _server = _provider.GetRequiredService<IOptionsMonitor<OpenIddictServerOptions>>().CurrentValue;
    }

    void Destroy() => _provider?.Dispose();
}

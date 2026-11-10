// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Cratis.Identity.for_TokenRateLimitMiddleware.given;

public class an_identity_host : Specification
{
    protected WebApplicationBuilder Builder;
    protected WebApplication App;

    void Establish()
    {
        Builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        Builder.Configuration.Sources.Clear();
        Builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Identity:Issuer"] = "https://localhost/",
            ["Identity:GitHub:ClientId"] = "spec-client",
            ["Identity:GitHub:ClientSecret"] = "spec-not-a-secret",
            ["Identity:TokenRateLimit:PerSourceRequests"] = "2",
            ["Identity:TokenRateLimit:Window"] = "01:00:00"
        });
        Builder.WebHost.UseTestServer();
        Builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;

            // Arc/Chronicle account command dependencies are outside this non-container host.
            options.ValidateOnBuild = false;
        });
        Builder.Services.AddSingleton(Substitute.For<IChronicleClient>());
    }

    protected void RegisterIdentity()
    {
        // Use the production entry point, leaving token exchange unconfigured.
        Builder.AddCratisIdentity();

        // These workers initialize storage, prune tokens and reconcile against external services.
        // Keep startup options validation and the real authentication/OpenIddict registrations.
        Builder.Services.RemoveAll<IHostedService>();

        // The upstream handler is real, but native account sign-in would require Chronicle.
        Builder.Services.Replace(ServiceDescriptor.Singleton(Substitute.For<IAccountSignIn>()));
    }

    async Task Destroy()
    {
        if (App is not null)
        {
            await App.DisposeAsync();
        }
    }
}

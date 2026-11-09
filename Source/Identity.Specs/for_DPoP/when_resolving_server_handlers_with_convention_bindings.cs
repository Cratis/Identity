// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.DependencyInjection;
using Cratis.Identity.DPoP;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenIddict.Server;

namespace Cratis.Identity.for_DPoP;

public class when_resolving_server_handlers_with_convention_bindings : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_resolve_every_handler_without_implicitly_enabling_dpop(bool enabled)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Identity:Issuer"] = "https://identity.example/",
            ["Identity:DPoP:Enabled"] = enabled.ToString(),
            ["Identity:DPoP:PublicTokenEndpoint"] = "https://identity.example/connect/token"
        });
        builder.Services.AddBindingsByConvention().AddSelfBindings();
        builder.Services.AddSingleton(Substitute.For<IChronicleClient>());
        builder.AddCratisIdentity();
        using var provider = builder.Services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        var services = scope.ServiceProvider;
        var handlers = services.GetRequiredService<IOptions<OpenIddictServerOptions>>().Value.Handlers;

        handlers.Count.ShouldBeGreaterThan(0);
        handlers.Count(handler => handler.ServiceDescriptor.ServiceType.Namespace == typeof(DPoPAdmission).Namespace).ShouldEqual(enabled ? 4 : 0);
        (services.GetService<DPoPAdmission>() is not null).ShouldEqual(enabled);
        foreach (var handler in handlers)
        {
            services.GetRequiredService(handler.ServiceDescriptor.ServiceType).ShouldNotBeNull();
        }
    }
}

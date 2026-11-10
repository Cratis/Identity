// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_IdentityServiceCollectionExtensions.given;

public class registered_features : Specification
{
    protected IServiceCollection enabled_services;
    protected IServiceCollection disabled_services;
    protected ServiceProvider enabled_provider;
    protected ServiceProvider disabled_provider;
    protected IServiceScope scope;

    void Establish()
    {
        enabled_services = Register(true);
        disabled_services = Register(false);
        enabled_provider = enabled_services.BuildServiceProvider();
        disabled_provider = disabled_services.BuildServiceProvider();
        scope = enabled_provider.CreateScope();
    }

    protected static string[] Snapshot(IServiceCollection services, IServiceProvider provider)
    {
        var descriptors = services.Select(Project).Order(StringComparer.Ordinal).Select(value => $"descriptor|{value}");
        var repeated = services.GroupBy(service => service.ServiceType).Where(group => group.Count() > 1)
            .OrderBy(group => group.Key.FullName, StringComparer.Ordinal)
            .SelectMany(group => group.Select(service => $"repeated|{Project(service)}"));
        var hosted = services.Where(service => service.ServiceType == typeof(IHostedService)).Select(service => $"hosted|{Project(service)}");
        var health = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations.Select(check => $"health|{check.Name}");
        return descriptors.Concat(repeated).Concat(hosted).Concat(health).ToArray();
    }

    static IServiceCollection Register(bool deviceEnabled)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IChronicleClient>());
        services.AddSingleton(Substitute.For<IReadModels>());
        services.AddSingleton(Substitute.For<IEventStore>());
        services.AddSingleton(Substitute.For<ICommandPipeline>());
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Identity:Issuer"] = "https://identity.example.test/",
            ["Identity:Device:Enabled"] = deviceEnabled.ToString(),
            ["Identity:DPoP:Enabled"] = deviceEnabled.ToString(),
            ["Identity:DPoP:PublicTokenEndpoint"] = "https://identity.example.test/connect/token"
        }).Build();
        var environment = new HostingEnvironment { EnvironmentName = Environments.Development };
        services.AddSingleton<IHostEnvironment>(environment);
        services.AddSingleton<IConfiguration>(configuration);
        services.AddCratisIdentity(configuration, environment);
        return services;
    }

    static string Project(ServiceDescriptor service) =>
        $"{service.ServiceType.FullName}|{service.Lifetime}|{Implementation(service)}";

    static string Implementation(ServiceDescriptor service)
    {
        if (service.ImplementationType is { } type) return type.FullName ?? type.Name;
        if (service.ImplementationInstance is not null) return "instance";
        return "factory";
    }

    void Destroy()
    {
        scope.Dispose();
        enabled_provider.Dispose();
        disabled_provider.Dispose();
    }
}

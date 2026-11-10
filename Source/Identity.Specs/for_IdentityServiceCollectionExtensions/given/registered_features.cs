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
        var descriptors = services.Where(IsIdentity).Select(Project).Order(StringComparer.Ordinal).Select(value => $"descriptor|{value}");
        var repeated = services.GroupBy(service => service.ServiceType).Where(group => group.Count() > 1)
            .OrderBy(group => group.Key.ToString(), StringComparer.Ordinal)
            .SelectMany(group => group.Select(service => $"repeated|{Project(service)}"));
        var hosted = services.Where(service => service.ServiceType == typeof(IHostedService)).Select(service => $"hosted|{Project(service)}");
        var health = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations.Select(check => $"health|{check.Name}");
        return descriptors.Concat(repeated).Concat(hosted).Concat(health).ToArray();
    }

    static IServiceCollection Register(bool deviceEnabled)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IReadModels>());
        services.AddSingleton(Substitute.For<IEventStore>());
        services.AddSingleton(Substitute.For<ICommandPipeline>());
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Identity:Issuer"] = "https://identity.example.test/",
            ["Identity:Device:Enabled"] = deviceEnabled.ToString()
        }).Build();
        services.AddCratisIdentity(configuration, new HostingEnvironment { EnvironmentName = Environments.Development });
        return services;
    }

    static bool IsIdentity(ServiceDescriptor service) =>
        service.ServiceType.ToString().Contains("Cratis.Identity.", StringComparison.Ordinal) ||
        (service.ImplementationType?.FullName?.StartsWith("Cratis.Identity.", StringComparison.Ordinal) ?? false) ||
        (service.ImplementationFactory?.Method.DeclaringType?.FullName?.StartsWith("Cratis.Identity.", StringComparison.Ordinal) ?? false);

    static string Project(ServiceDescriptor service) =>
        $"{service.ServiceType}|{service.Lifetime}|{Implementation(service)}";

    static string Implementation(ServiceDescriptor service)
    {
        if (service.ImplementationType is { } type) return type.ToString();
        if (service.ImplementationInstance is { } instance) return instance.GetType().ToString();
        return "<factory>";
    }

    void Destroy()
    {
        scope.Dispose();
        enabled_provider.Dispose();
        disabled_provider.Dispose();
    }
}

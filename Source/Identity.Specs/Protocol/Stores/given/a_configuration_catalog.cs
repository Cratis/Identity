// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Identity.Clients;
using Cratis.Identity.Configuration;
using Cratis.Identity.Hosting;
using Cratis.Identity.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace Cratis.Identity.Protocol.Stores.given;

public class a_configuration_catalog : Specification
{
    protected const string Secret = "spec-only-introspection-canary";
    protected ServiceProvider Provider;
    protected IServiceScope Scope;
    internal IOpenIddictApplicationStore<ClientApplication> Applications;
    internal IOpenIddictScopeStore<ResourceScope> Scopes;
    protected IOpenIddictApplicationManager Manager;
    internal ClientDescriptors Descriptors;
    internal IResourceCatalog Resources;
    protected ConcurrentQueue<string> Logs = new();

    void Establish()
    {
        var options = Options.Create(new IdentityOptions
        {
            IntrospectionClients = [new() { ClientId = "spec-api", ClientSecret = Secret }],
            Resources = [new()
            {
                Audience = "spec-api",
                Indicators = ["https://api.example/"],
                Scopes = [new() { Name = "spec:read", Description = "Read" }, new() { Name = "spec:write", Description = "Write" }]
            }]
        });
        Resources = new ResourceCatalog(options);
        Descriptors = new ClientDescriptors(Resources, options);
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(new LogProvider(Logs)));
        services.AddSingleton(Resources);
        services.AddSingleton(Descriptors);
        services.AddOpenIddict().AddCore(core => core.UseConfigurationStores());
        Provider = services.BuildServiceProvider();
        Scope = Provider.CreateScope();
        Applications = Scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationStore<ClientApplication>>();
        Scopes = Scope.ServiceProvider.GetRequiredService<IOpenIddictScopeStore<ResourceScope>>();
        Manager = Scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
    }

    sealed class LogProvider(ConcurrentQueue<string> messages) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new Log(messages);
        public void Dispose() { }
    }

    sealed class Log(ConcurrentQueue<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => messages.Enqueue(formatter(state, exception));
    }

    async Task Destroy()
    {
        Scope.Dispose();
        await Provider.DisposeAsync();
    }
}

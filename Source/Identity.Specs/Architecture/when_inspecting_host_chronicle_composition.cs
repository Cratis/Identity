// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Architecture;

public class when_inspecting_host_chronicle_composition : Specification
{
    string _program;
    IChronicleClient _client;
    ServiceProvider _services;

    void Establish()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Identity.slnx")))
        {
            directory = directory.Parent;
        }
        var root = directory?.FullName ?? throw new InvalidOperationException("Identity.slnx was not found above the specification output.");
        _program = File.ReadAllText(Path.Combine(root, "Source", "Identity.Host", "Program.cs"));

        var builder = WebApplication.CreateBuilder();
        builder.AddCratisChronicle(options => options.EventStore = "Identity");
        builder.Services.Configure<ChronicleClientOptions>(options => options.EventStore = "Identity");
        builder.Services.AddCommandAwareDecisionReads();
        _client = Substitute.For<IChronicleClient>();
        _client.GetEventStore(Arg.Any<EventStoreName>()).Returns(Task.FromException<IEventStore>(new ConnectionUnavailable("Unavailable")));
        builder.Services.AddSingleton(_client);
        _services = builder.Services.BuildServiceProvider();
    }

    async Task Because()
    {
        // Arc keeps this startup-only interface internal. Exercise its registered implementation without starting a host or connecting to Chronicle.
        var admission = _services.GetRequiredService(typeof(ReadModelServiceCollectionExtensions).Assembly
            .GetType("Cratis.Arc.Chronicle.ReadModels.IStartupDecisionReads", throwOnError: true));
        var get = admission.GetType().GetMethod("Get");
        await (Task)get.Invoke(admission, [CancellationToken.None]);
    }

    [Fact] void should_configure_the_host_client_options_used_by_startup_admission() => _program.ShouldContain("builder.Services.Configure<ChronicleClientOptions>(options => options.EventStore = \"Identity\");");
    [Fact] void should_configure_the_same_store_for_requests() => _program.ShouldContain("options.EventStore = \"Identity\";");
    [Fact] void should_request_the_configured_identity_store_for_startup_admission() => _client.Received(1).GetEventStore(Arg.Is<EventStoreName>(name => name.Value == "Identity"));
    [Fact] void should_never_request_an_empty_event_store() => _client.DidNotReceive().GetEventStore(Arg.Is<EventStoreName>(name => string.IsNullOrWhiteSpace(name.Value)));

    void Destroy() => _services.Dispose();
}

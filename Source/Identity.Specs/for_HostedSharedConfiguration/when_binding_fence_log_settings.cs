// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Identity.Revocation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Identity.for_HostedSharedConfiguration;

public class when_binding_fence_log_settings : Specification
{
    [Fact]
    void should_ship_the_runtime_fence_log_section()
    {
        var configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("CratisId.appsettings.json").Build();
        configuration.GetSection("Identity:FenceLog").Exists().ShouldBeTrue();
        configuration.GetSection("Identity:Revocation:FenceLog").Exists().ShouldBeFalse();
    }

    [Fact]
    async Task should_resolve_the_documented_settings_through_production_registration()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Identity:FenceLog:ConnectionString"] = "mongodb://independent.example",
            ["Identity:FenceLog:Database"] = "synthetic-fences",
            ["Identity:FenceLog:InstanceId"] = "synthetic-independent-custody",
            ["Identity:FenceLog:MinimumSequence"] = "73",
            ["Identity:FenceLog:AllowSharedDeployment"] = "true"
        });
        builder.Services.AddSingleton(Substitute.For<IChronicleClient>());
        builder.AddCratisIdentity();
        await using var provider = builder.Services.BuildServiceProvider();
        var options = provider.GetRequiredService<FenceLogOptions>();
        options.ConnectionString.ShouldEqual("mongodb://independent.example");
        options.Database.ShouldEqual("synthetic-fences");
        options.InstanceId.ShouldEqual("synthetic-independent-custody");
        options.MinimumSequence.ShouldEqual(73L);
        options.AllowSharedDeployment.ShouldBeTrue();
    }

    [Fact]
    void should_refuse_a_shared_deployment_unless_the_operator_allows_it() => new FenceLogOptions().AllowSharedDeployment.ShouldBeFalse();
}

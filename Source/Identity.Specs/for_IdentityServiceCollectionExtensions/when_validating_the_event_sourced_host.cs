// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Identity.for_IdentityServiceCollectionExtensions;

public class when_validating_the_event_sourced_host
{
    [Fact]
    void should_build_the_complete_host_service_collection()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.Configuration["Identity:Clients:EventSourced"] = "true";
        builder.AddCratisChronicle(options => options.EventStore = "identity");
        builder.AddCratisIdentity();

        using var provider = builder.Services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.ShouldNotBeNull();
    }
}

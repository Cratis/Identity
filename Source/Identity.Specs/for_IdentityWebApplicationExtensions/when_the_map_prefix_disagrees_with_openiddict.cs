// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_IdentityWebApplicationExtensions;

public class when_the_map_prefix_disagrees_with_openiddict : given.an_identity_host
{
    [Fact]
    async Task should_refuse_to_map_and_preserve_the_registered_prefix()
    {
        var builder = CreateBuilder("/id");
        builder.AddCratisIdentity();
        await using var app = builder.Build();
        var exception = Catch.Exception(() => app.MapCratisIdentity(options => options.RoutePrefix = "/other"));
        exception.ShouldBeOfExactType<InvalidOperationException>();
        exception.Message.ShouldContain("/id");
        exception.Message.ShouldContain("/other");
        ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).ShouldBeEmpty();
        app.Services.GetRequiredService<IOptions<IdentityEndpointOptions>>().Value.RoutePrefix.Value.ShouldEqual("/id");
    }
}

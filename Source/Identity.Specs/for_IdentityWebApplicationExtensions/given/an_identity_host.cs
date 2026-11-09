// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Identity.for_IdentityWebApplicationExtensions.given;

public class an_identity_host : Specification
{
    protected static WebApplicationBuilder CreateBuilder(string prefix = "")
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.Host.UseDefaultServiceProvider(options => options.ValidateOnBuild = false);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Identity:Endpoints:RoutePrefix"] = prefix,
            ["Identity:Issuer"] = "https://identity.example/",
            ["Identity:GitHub:ClientId"] = "spec-client",
            ["Identity:GitHub:ClientSecret"] = "spec-only-placeholder"
        });
        builder.Services.AddSingleton(Substitute.For<IChronicleClient>());
        return builder;
    }

    protected static async Task<DefaultHttpContext> Request(WebApplication app, string path, string method = "GET")
    {
        await using var scope = app.Services.CreateAsyncScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("identity.example");
        context.RequestAborted = TestContext.Current.CancellationToken;
        context.Response.Body = new MemoryStream();
        await ((IApplicationBuilder)app).Build()(context);
        return context;
    }
}

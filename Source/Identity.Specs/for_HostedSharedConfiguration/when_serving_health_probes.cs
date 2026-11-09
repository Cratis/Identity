// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Identity.Revocation;
using Cratis.Identity.Server;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Cratis.Identity.for_HostedSharedConfiguration;

public class when_serving_health_probes : Specification
{
    [Fact]
    async Task should_keep_liveness_separate_from_recovery_readiness()
    {
        await using var app = CreateApp();
        var gate = app.Services.GetRequiredService<RecoveryGate>();
        (await Request(app, "/healthz")).ShouldEqual(StatusCodes.Status200OK);
        (await Request(app, "/readyz")).ShouldEqual(StatusCodes.Status503ServiceUnavailable);
        gate.Open(true);
        (await Request(app, "/readyz")).ShouldEqual(StatusCodes.Status200OK);
        gate.Close();
        (await Request(app, "/readyz")).ShouldEqual(StatusCodes.Status503ServiceUnavailable);
        (await Request(app, "/healthz")).ShouldEqual(StatusCodes.Status200OK);
    }

    [Fact]
    async Task should_fail_closed_when_no_recovery_check_matches()
    {
        await using var app = CreateApp(removeRecoveryCheck: true);
        app.Services.GetRequiredService<RecoveryGate>().Open(true);
        (await Request(app, "/readyz")).ShouldEqual(StatusCodes.Status503ServiceUnavailable);
        (await Request(app, "/healthz")).ShouldEqual(StatusCodes.Status200OK);
    }

    static WebApplication CreateApp(bool removeRecoveryCheck = false)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.Host.UseDefaultServiceProvider(options => options.ValidateOnBuild = false);
        builder.Services.AddSingleton(Substitute.For<IChronicleClient>());
        builder.AddCratisIdentity();

        // Probe requests do not exercise authentication. Keep the production recovery registrations intact.
        builder.Services.AddSingleton(Substitute.For<IAuthenticationSchemeProvider>());
        builder.Services.AddHealthChecks().AddCheck("unrelated", () => HealthCheckResult.Unhealthy());
        if (removeRecoveryCheck)
        {
            builder.Services.Configure<HealthCheckServiceOptions>(options =>
            {
                foreach (var check in options.Registrations.Where(check => check.Name != "unrelated").ToArray())
                {
                    options.Registrations.Remove(check);
                }
            });
        }

        var app = builder.Build();
        app.MapCratisIdentity();
        HostedSharedHosting.MapReadiness(app);
        app.Run(context => context.GetEndpoint().RequestDelegate(context));
        return app;
    }

    static async Task<int> Request(WebApplication app, string path)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = path;
        context.RequestAborted = TestContext.Current.CancellationToken;
        await using var body = new MemoryStream();
        context.Response.Body = body;
        await ((IApplicationBuilder)app).Build()(context);
        return context.Response.StatusCode;
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Revocation;
using Cratis.Identity.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Identity.for_HostedSharedConfiguration;

public class when_serving_health_probes : Specification
{
    [Fact]
    async Task should_keep_liveness_separate_from_recovery_readiness()
    {
        var builder = WebApplication.CreateBuilder();
        var gate = new RecoveryGate();
        builder.Services.AddSingleton(gate);
        builder.Services.AddHealthChecks().AddCheck<RecoveryReadiness>("identity-recovery")
            .AddCheck("unrelated", () => HealthCheckResult.Unhealthy());
        builder.Services.AddAuthentication();
        builder.Services.AddAntiforgery();
        builder.Services.AddTransient<AuthorizationEndpoint>();
        builder.Services.AddTransient<TokenEndpoint>();
        await using var app = builder.Build();
        app.MapCratisIdentity();
        HostedSharedHosting.MapReadiness(app);
        app.Run(context => context.GetEndpoint().RequestDelegate(context));
        var pipeline = ((IApplicationBuilder)app).Build();
        await using var scope = app.Services.CreateAsyncScope();
        (await Request("/healthz")).ShouldEqual(StatusCodes.Status200OK);
        (await Request("/readyz")).ShouldEqual(StatusCodes.Status503ServiceUnavailable);
        gate.Open(true);
        (await Request("/readyz")).ShouldEqual(StatusCodes.Status200OK);
        gate.Close();
        (await Request("/readyz")).ShouldEqual(StatusCodes.Status503ServiceUnavailable);
        (await Request("/healthz")).ShouldEqual(StatusCodes.Status200OK);

        async Task<int> Request(string path)
        {
            var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = path;
            context.RequestAborted = TestContext.Current.CancellationToken;
            await using var body = new MemoryStream();
            context.Response.Body = body;
            await pipeline(context);
            return context.Response.StatusCode;
        }
    }
}

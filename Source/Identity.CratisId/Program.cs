// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Identity.Server;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.AddCratisArc(options => options.GeneratedApis.SegmentsToSkipForRoute = 2);
builder.AddCratisChronicle(options => builder.Configuration.GetSection("Cratis:Chronicle").Bind(options));
try
{
    builder.AddCratisIdentity();
}
catch (OptionsValidationException exception)
{
    await Console.Error.WriteLineAsync("CratisId hosted-shared configuration is incomplete:");
    foreach (var failure in exception.Failures)
    {
        await Console.Error.WriteLineAsync($"- {failure}");
    }

    return 1;
}

HostedSharedHosting.AddProxyHeaders(builder);

var app = builder.Build();
app.UseForwardedHeaders();
app.UseCratisIdentity();
app.MapCratisIdentity(options => options.MapSignOut = true);
app.MapGet("/healthz", () => Results.Text("ok"));
HostedSharedHosting.MapReadiness(app);
app.UseCratisArc();
app.UseCratisChronicle();
await app.RunAsync();
return 0;

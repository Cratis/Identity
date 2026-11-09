// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.AddCratisArc(options => options.GeneratedApis.SegmentsToSkipForRoute = 2);
builder.AddCratisChronicle(options =>
{
    options.EventStore = "Identity";
    options.ConnectionString = builder.Configuration.GetConnectionString("chronicle") ?? builder.Configuration["Chronicle:ConnectionString"] ?? "chronicle://localhost:35000";
});

builder.AddCratisIdentity();

var app = builder.Build();
app.UseCratisIdentity();
app.MapCratisIdentity(options => options.MapSignOut = true);
app.MapGet("/healthz", () => Results.Text("ok"));
app.UseCratisArc();
app.UseCratisChronicle();
await app.RunAsync();

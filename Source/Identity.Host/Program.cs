// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

if (builder.Configuration.GetValue<bool>("Identity:Clients:EventSourced"))
{
    builder.AddCratisChronicle(options =>
    {
        options.EventStore = "identity";
        var connection = builder.Configuration["Identity:Chronicle:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(connection))
        {
            options.ConnectionString = connection;
        }
    });
}

builder.AddCratisIdentity();

var app = builder.Build();
app.MapCratisIdentity();
await app.RunAsync();

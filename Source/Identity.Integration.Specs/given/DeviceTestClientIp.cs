// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Cratis.Identity.Integration.given;

/// <summary>Supplies the peer address that TestServer's in-memory transport does not provide.</summary>
public sealed class DeviceTestClientIp : IStartupFilter
{
    /// <inheritdoc/>
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, continuation) =>
        {
            if (context.Connection.RemoteIpAddress is null)
            {
                context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.79");
            }

            await continuation(context);
        });
        next(app);
    };
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;
using OpenIddict.Abstractions;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Sessions;

/// <summary>Hands only end-session requests to Identity's ID-token-only validation boundary.</summary>
internal static class EndSessionServerExtensions
{
    /// <summary>Registers the prefixed endpoint only for hosts with an explicit issuer.</summary>
    /// <param name="server">The server builder.</param>
    /// <param name="routePrefix">The Identity route prefix.</param>
    /// <param name="options">The startup Identity options.</param>
    /// <returns>The server builder for continuation.</returns>
    internal static OpenIddictServerBuilder AddEndSessionEndpoint(this OpenIddictServerBuilder server, string routePrefix, IdentityOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            return server;
        }

        return server.SetEndSessionEndpointUris($"{routePrefix}/connect/endsession".TrimStart('/'))
            .AddEventHandler<ExtractEndSessionRequestContext>(builder => builder
                .SetOrder(int.MinValue + 100)
                .UseInlineHandler(context =>
                {
                    context.SkipRequest();
                    return ValueTask.CompletedTask;
                }));
    }
}

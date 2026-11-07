// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Device;

/// <summary>Renders uniform verification failures and preserves HTTP throttle status.</summary>
/// <param name="accessor">The current HTTP request.</param>
internal sealed class DeviceVerificationResponse(IHttpContextAccessor accessor) : IOpenIddictServerHandler<ApplyEndUserVerificationResponseContext>
{
    /// <summary>Gets the descriptor before the host serializes protocol errors.</summary>
    internal static OpenIddictServerHandlerDescriptor Descriptor { get; } = OpenIddictServerHandlerDescriptor.CreateBuilder<ApplyEndUserVerificationResponseContext>()
        .UseScopedHandler<DeviceVerificationResponse>().SetOrder(int.MinValue + 100_000)
        .SetType(OpenIddictServerHandlerType.Custom).Build();

    /// <inheritdoc/>
    public async ValueTask HandleAsync(ApplyEndUserVerificationResponseContext context)
    {
        var http = accessor.HttpContext;
        if (http is null || string.IsNullOrEmpty(context.Response.Error)) return;
        http.Response.Headers.CacheControl = "no-store";
        http.Response.Headers.Pragma = "no-cache";
        http.Response.Headers["Referrer-Policy"] = "no-referrer";
        var result = context.Response.Error == Errors.SlowDown ? DeviceGrantPages.Throttled() : DeviceGrantPages.Unavailable();
        await result.ExecuteAsync(http);
        context.HandleRequest();
    }
}

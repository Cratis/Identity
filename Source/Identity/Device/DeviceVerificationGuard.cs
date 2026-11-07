// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.Antiforgery;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.Device;

/// <summary>CSRF, session, readiness and shared rate admission precede the stock user-code lookup.</summary>
/// <param name="accessor">The current HTTP request.</param>
/// <param name="antiforgery">The host antiforgery service.</param>
/// <param name="accounts">The active-account authority.</param>
/// <param name="rates">The shared rate windows.</param>
/// <param name="admission">The recovery authority.</param>
internal sealed class DeviceVerificationGuard(IHttpContextAccessor accessor, IAntiforgery antiforgery, SignedInAccounts accounts, DeviceGrantRateLimiter rates, DeviceGrantAdmission admission) : IOpenIddictServerHandler<ProcessAuthenticationContext>
{
    /// <summary>Gets the descriptor before any user-code validation or database lookup.</summary>
    internal static OpenIddictServerHandlerDescriptor Descriptor { get; } = OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessAuthenticationContext>()
        .UseScopedHandler<DeviceVerificationGuard>()
        .SetOrder(OpenIddictServerHandlers.ValidateUserCode.Descriptor.Order - 1)
        .SetType(OpenIddictServerHandlerType.Custom).Build();

    /// <inheritdoc/>
    public async ValueTask HandleAsync(ProcessAuthenticationContext context)
    {
        if (context.EndpointType != OpenIddictServerEndpointType.EndUserVerification) return;
        var http = accessor.HttpContext;
        if (http is null || !await admission.Ready(context.CancellationToken))
        {
            context.Reject(Errors.TemporarilyUnavailable, "Device verification is unavailable.");
            return;
        }

        http.Response.Headers.CacheControl = "no-store";
        http.Response.Headers.Pragma = "no-cache";
        http.Response.Headers["Referrer-Policy"] = "no-referrer";
        if (HttpMethods.IsGet(http.Request.Method) && string.IsNullOrEmpty(context.UserCode)) return;
        if (!HttpMethods.IsPost(http.Request.Method) || !await antiforgery.IsRequestValidAsync(http))
        {
            context.Reject(Errors.InvalidRequest, "The verification request is not valid.");
            return;
        }

        var account = await accounts.Find(http, new OpenIddict.Abstractions.OpenIddictRequest());
        if (account is null)
        {
            context.Reject(Errors.AccessDenied, "The verification request cannot be completed.");
            return;
        }

        if (!await rates.Verify(context.UserCode, account.Id.Value, http.Connection.RemoteIpAddress?.ToString(), context.CancellationToken))
        {
            http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            http.Response.Headers.RetryAfter = "300";
            context.Reject(Errors.SlowDown, "Device verification attempt limit reached.");
            return;
        }

        var normalized = rates.Normalize(context.UserCode);
        if (normalized is null)
        {
            context.Reject(Errors.AccessDenied, "The verification request cannot be completed.");
            return;
        }

        context.UserCode = normalized;
        context.Request.UserCode = normalized;
    }
}

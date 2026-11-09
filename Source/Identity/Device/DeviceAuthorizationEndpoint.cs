// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Authorization;
using Cratis.Identity.Configuration;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Device;

/// <summary>Starts a subjectless, short-lived request after OpenIddict validates the client.</summary>
/// <param name="policy">The shared single-resource grant policy.</param>
/// <param name="applications">The client manager.</param>
/// <param name="admission">The device recovery authority.</param>
/// <param name="options">The device flow switch.</param>
/// <param name="rates">The shared issuance budget.</param>
internal sealed class DeviceAuthorizationEndpoint(IGrantPolicy policy, IOpenIddictApplicationManager applications, DeviceGrantAdmission admission, IOptions<IdentityOptions> options, DeviceGrantRateLimiter rates)
{
    /// <summary>Retains the resource indicator inside the protected request, not the public access token.</summary>
    internal const string ResourceClaim = "oi_cratis_device_resource";

    /// <summary>Maps the device initiation route.</summary>
    /// <param name="endpoints">The host routes.</param>
    internal static void Map(IEndpointRouteBuilder endpoints) => endpoints.MapPost("/connect/device", (HttpContext context, [FromServices] DeviceAuthorizationEndpoint endpoint) => endpoint.Handle(context)).DisableAntiforgery();

    static DeviceAuthorizationError Failure(string error) => new(error, "The device authorization request cannot be completed.");

    async Task<IResult> Handle(HttpContext context)
    {
        if (!options.Value.DeviceGrant.Enabled) return Results.NotFound();
        if (!await admission.Ready(context.RequestAborted)) return Failure(Errors.TemporarilyUnavailable);
        var request = context.GetOpenIddictServerRequest();
        if (request is null || string.IsNullOrEmpty(request.ClientId) || await applications.FindByClientIdAsync(request.ClientId, context.RequestAborted) is null)
        {
            return Failure(Errors.InvalidClient);
        }

        var evaluation = policy.Evaluate(new GrantRequest(request.GetScopes(), request.GetResources()));
        if (evaluation is NotGrantable rejected) return new DeviceAuthorizationError(rejected.Error, rejected.Description);
        if (!await rates.Issue(context.Connection.RemoteIpAddress?.ToString(), context.RequestAborted))
        {
            context.Response.Headers.RetryAfter = "60";
            return Results.Json(new { error = Errors.SlowDown }, statusCode: StatusCodes.Status429TooManyRequests);
        }

        var grant = ((Grantable)evaluation).Grant;
        var principal = new ClaimsPrincipal(new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme));
        principal.SetScopes(grant.Scopes);
        principal.SetResources(grant.Resource.Audience);
        principal.SetClaim(ResourceClaim, grant.Resource.Indicators.First());
        return Results.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}

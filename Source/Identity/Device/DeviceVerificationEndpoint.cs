// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Cratis.Identity.Authorization;
using Cratis.Identity.Configuration;
using Cratis.Identity.Tenants;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Device;

/// <summary>Explicit session-bound, CSRF-protected device consent; GET never approves access.</summary>
/// <param name="accounts">The signed-in account authority.</param>
/// <param name="antiforgery">The antiforgery service.</param>
/// <param name="applications">The client manager.</param>
/// <param name="tokens">The token manager.</param>
/// <param name="policy">The shared resource and scope policy.</param>
/// <param name="admission">The recovery authority.</param>
/// <param name="approval">The session-bound consent evidence.</param>
/// <param name="tenants">The live tenant membership authority.</param>
/// <param name="upstream">The host's sign-in scheme.</param>
/// <param name="options">The device flow switch.</param>
/// <param name="time">The clock.</param>
internal sealed class DeviceVerificationEndpoint(SignedInAccounts accounts, IAntiforgery antiforgery, IOpenIddictApplicationManager applications, IOpenIddictTokenManager tokens, IGrantPolicy policy, DeviceGrantAdmission admission, DeviceGrantApproval approval, ITenantDirectory tenants, IOptions<UpstreamLoginOptions> upstream, IOptions<IdentityOptions> options, TimeProvider time)
{
    /// <summary>Maps manual verification and confirmation.</summary>
    /// <param name="endpoints">The host routes.</param>
    internal static void Map(IEndpointRouteBuilder endpoints) => endpoints.MapMethods("/device", [HttpMethods.Get, HttpMethods.Post], (HttpContext context, [FromServices] DeviceVerificationEndpoint endpoint) => endpoint.Handle(context)).DisableAntiforgery();

    static void Secure(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers.Pragma = "no-cache";
        response.Headers["Referrer-Policy"] = "no-referrer";
        response.Headers.ContentSecurityPolicy = "default-src 'none'; form-action 'self'; frame-ancestors 'none'; base-uri 'none'";
    }

    string IssuerHost(HttpContext context) => string.IsNullOrEmpty(options.Value.Issuer) ? context.Request.Host.Host : new Uri(options.Value.Issuer).Host;

    async Task<IResult> Handle(HttpContext context)
    {
        Secure(context.Response);
        if (!options.Value.DeviceGrant.Enabled) return Results.NotFound();
        var account = await accounts.Find(context, new OpenIddictRequest());
        if (account is null)
        {
            return Results.Challenge(new AuthenticationProperties { RedirectUri = "/device" }, [upstream.Value.Scheme]);
        }

        if (HttpMethods.IsGet(context.Request.Method))
        {
            return context.Request.Query.ContainsKey("done") ? DeviceGrantPages.Complete() : DeviceGrantPages.Entry(antiforgery.GetAndStoreTokens(context), IssuerHost(context));
        }

        if (!await antiforgery.IsRequestValidAsync(context)) return DeviceGrantPages.Unavailable();
        var authentication = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var principal = authentication.Principal;
        if (!authentication.Succeeded || principal is null || !await admission.CanUse(principal, false, context.RequestAborted)) return DeviceGrantPages.Unavailable();
        var clientId = principal.GetClaim(Claims.ClientId);
        if (string.IsNullOrEmpty(clientId)) return DeviceGrantPages.Unavailable();
        var client = await applications.FindByClientIdAsync(clientId, context.RequestAborted);
        if (client is null) return DeviceGrantPages.Unavailable();
        var indicator = principal.GetClaim(DeviceAuthorizationEndpoint.ResourceClaim);
        if (string.IsNullOrEmpty(indicator)) return DeviceGrantPages.Unavailable();
        var evaluation = policy.Evaluate(new GrantRequest(principal.GetScopes(), [indicator]));
        if (evaluation is not Grantable grantable) return DeviceGrantPages.Unavailable();
        var grant = grantable.Grant;
        if (!grant.Scopes.ToHashSet(StringComparer.Ordinal).SetEquals(principal.GetScopes())) return DeviceGrantPages.Unavailable();
        var lookup = await tenants.SelectableTenants(account, grant.Resource, context.RequestAborted);
        if (lookup is not TenantsListed listed) return DeviceGrantPages.Unavailable();
        var selectable = listed.Tenants.ToArray();
        if (selectable.Length == 0) return DeviceGrantPages.Unavailable();
        var cookie = new ChunkingCookieManager().GetRequestCookie(context, "cratis-identity-session");
        if (string.IsNullOrEmpty(cookie)) return DeviceGrantPages.Unavailable();
        var binding = string.Join('\0', principal.GetTokenId(), account.Id.Value, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cookie))), clientId, grant.Resource.Audience, string.Join(' ', grant.Scopes.Order(StringComparer.Ordinal)), string.Join(' ', selectable.Select(_ => _.Id).Order(StringComparer.Ordinal)));
        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        var action = form["action"].ToString();
        if (action == "review")
        {
            var name = await applications.GetDisplayNameAsync(client, context.RequestAborted) ?? clientId!;
            return DeviceGrantPages.Review(name, grant, DeviceGrantUserCode.Normalize(form[Parameters.UserCode], options.Value.DeviceGrant.UserCodeLength, options.Value.DeviceGrant.UserCodeCharset)!, approval.Create(binding), selectable, antiforgery.GetAndStoreTokens(context), IssuerHost(context), clientId, account.GitHubLogin.Value);
        }

        if ((action != "approve" && action != "deny") || !approval.Matches(form["proof"], binding)) return DeviceGrantPages.Unavailable();
        var properties = new AuthenticationProperties { RedirectUri = "/device?done=1" };
        if (action == "deny")
        {
            properties.Items[OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.AccessDenied;
            return Results.Forbid(properties, [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
        }

        var device = await tokens.FindByIdAsync(principal.GetClaim(Claims.Private.DeviceCodeId)!, context.RequestAborted);
        var expires = device is null ? null : await tokens.GetExpirationDateAsync(device, context.RequestAborted);
        if (expires is null || expires <= time.GetUtcNow()) return DeviceGrantPages.Unavailable();
        var tenant = selectable.FirstOrDefault(_ => string.Equals(_.Id, form["tenant"], StringComparison.Ordinal));
        if (tenant is null) return DeviceGrantPages.Unavailable();
        context.GetOpenIddictServerRequest()!.ClientId = clientId;
        var identity = AccessTokenIdentity.Create(account, clientId, grant, tenant);
        identity.SetPresenters(clientId);
        identity.SetClaim(Claims.Private.DeviceCodeId, principal.GetClaim(Claims.Private.DeviceCodeId));
        principal = new ClaimsPrincipal(identity);
        principal.SetDeviceCodeLifetime(expires.Value - time.GetUtcNow());
        return Results.SignIn(principal, properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}

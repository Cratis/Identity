// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using OpenIddict.Abstractions;

namespace Cratis.Identity.Sessions;

/// <summary>Ends only this browser's Identity cookie session, never its grants or relying-party sessions.</summary>
/// <param name="hints">The ID-token-only hint validator.</param>
/// <param name="applications">The registered relying parties.</param>
/// <param name="antiforgery">The confirmation form protection.</param>
internal class EndSessionEndpoint(EndSessionHintValidator hints, IOpenIddictApplicationManager applications, IAntiforgery antiforgery)
{
    /// <summary>Handles RP-initiated logout without the access-token authentication pipeline.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A confirmation, error, local completion or validated relying-party redirect.</returns>
    public async Task<IResult> Handle(HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        var request = await EndSessionRequest.Read(context.Request);
        if (request is null)
        {
            return Results.BadRequest("The sign-out form expired or did not come from this browser session.");
        }

        var hint = request.Hint is null ? null : await hints.Validate(request.Hint, context.RequestAborted);
        if (!AcceptsHint(request, hint) || !await IsKnownClient(request.ClientId, context.RequestAborted))
        {
            return Results.BadRequest("The end-session hint or client is invalid.");
        }

        if (!await AcceptsRedirect(request.RedirectUri, hint, context.RequestAborted))
        {
            return EndSessionPage.Confirm(context, antiforgery, new(null, null, null, null), invalidRedirect: true);
        }

        if (await NeedsConfirmation(context, hint))
        {
            return EndSessionPage.Confirm(context, antiforgery, request);
        }

        return await Complete(context, request);
    }

    async Task<bool> NeedsConfirmation(HttpContext context, EndSessionHint? hint) =>
        HttpMethods.IsPost(context.Request.Method)
            ? !await antiforgery.IsRequestValidAsync(context)
            : !await MatchesSession(context, hint);

    static async Task<IResult> Complete(HttpContext context, EndSessionRequest request)
    {
        await context.SignOutAsync(IdentitySchemes.Session);
        if (request.RedirectUri is null)
        {
            return Results.Text("Signed out.");
        }

        var redirect = request.State is null ? request.RedirectUri : QueryHelpers.AddQueryString(request.RedirectUri, "state", request.State);
        return Results.Redirect(redirect);
    }

    static bool AcceptsHint(EndSessionRequest request, EndSessionHint? hint) =>
        HasRequiredHint(request, hint) && MatchesClient(request.ClientId, hint);

    static bool HasRequiredHint(EndSessionRequest request, EndSessionHint? hint) =>
        hint is not null || (request.Hint is null && request.RedirectUri is null);

    static bool MatchesClient(string? clientId, EndSessionHint? hint) =>
        clientId is null || hint is null || string.Equals(clientId, hint.ClientId, StringComparison.Ordinal);

    async Task<bool> IsKnownClient(string? clientId, CancellationToken cancellationToken) =>
        clientId is null || await applications.FindByClientIdAsync(clientId, cancellationToken) is not null;

    async Task<bool> AcceptsRedirect(string? redirect, EndSessionHint? hint, CancellationToken cancellationToken)
    {
        if (redirect is null)
        {
            return true;
        }

        return hint is not null && IsAbsoluteRedirectWithoutFragment(redirect) &&
            (await applications.GetPostLogoutRedirectUrisAsync(hint.Application, cancellationToken)).Contains(redirect, StringComparer.Ordinal);
    }

    static bool IsAbsoluteRedirectWithoutFragment(string redirect) =>
        Uri.TryCreate(redirect, UriKind.Absolute, out var uri) && string.IsNullOrEmpty(uri.Fragment);

    static async Task<bool> MatchesSession(HttpContext context, EndSessionHint? hint)
    {
        if (hint is null || string.IsNullOrEmpty(hint.SessionId))
        {
            return false;
        }

        var session = await context.AuthenticateAsync(IdentitySchemes.Session);
        return session.Succeeded && session.Principal is { } principal &&
            string.Equals(hint.SessionId, SessionPrincipal.SessionId(principal), StringComparison.Ordinal) &&
            string.Equals(hint.Subject, SessionPrincipal.AccountId(principal), StringComparison.Ordinal);
    }
}

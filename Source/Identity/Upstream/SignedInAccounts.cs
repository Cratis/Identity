// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization;
using Cratis.Identity.Contracts.Accounts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Upstream;

/// <summary>
/// Resolves the account signed in to the Identity site, and sends the browser to upstream login when there is none.
/// </summary>
/// <param name="accounts">The active-account reader.</param>
/// <param name="upstream">The <see cref="UpstreamLoginOptions"/>.</param>
/// <param name="time">The clock used to evaluate the requested maximum authentication age, or the system clock when omitted.</param>
internal class SignedInAccounts(IAccountReader accounts, IOptions<UpstreamLoginOptions> upstream, TimeProvider? time = null)
{
    /// <summary>
    /// Finds the account signed in to the Identity site for an authorization request.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="request">The <see cref="OpenIddictRequest"/>.</param>
    /// <returns>The <see cref="Account"/> with its upstream authentication time, or <see langword="null"/> when the user must sign in (again), including a session without an authentication time.</returns>
    public async Task<SignedInAccount?> Find(HttpContext context, OpenIddictRequest request)
    {
        var session = await context.AuthenticateAsync(IdentitySchemes.Session);
        if (!session.Succeeded || request.HasPromptValue(PromptValues.Login))
        {
            return null;
        }

        var accountId = SessionPrincipal.AccountId(session.Principal);
        var authTime = SessionPrincipal.AuthTime(session.Principal);
        if (authTime is null || RequiresReauthentication(request, authTime.Value) || !AccountSubject.TryParse(accountId, out var subject))
        {
            return null;
        }

        var account = await accounts.FindActive(subject!, context.RequestAborted);
        return account is null ? null : new SignedInAccount(account, authTime.Value, SessionPrincipal.SessionId(session.Principal));
    }

    bool RequiresReauthentication(OpenIddictRequest request, DateTimeOffset authTime) =>
        request.MaxAge is { } maximum && (maximum == 0 || ((time ?? TimeProvider.System).GetUtcNow() - authTime).TotalSeconds > maximum);

    /// <summary>
    /// Sends the browser to upstream login, returning to the same authorization request afterwards.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>The <see cref="IResult"/>.</returns>
    public IResult Challenge(HttpContext context)
    {
        var parameters = AuthorizationParameters.AfterLogin(AuthorizationParameters.From(context.Request))
            .Select(_ => new KeyValuePair<string, string?>(_.Key, _.Value));
        var returnUrl = $"{context.Request.PathBase}{context.Request.Path}{QueryString.Create(parameters)}";
        return Results.Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, [upstream.Value.Scheme]);
    }
}

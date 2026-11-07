// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Upstream;

/// <summary>
/// Resolves the account signed in to the Identity site, and sends the browser to upstream login when there is none.
/// </summary>
/// <param name="accounts">The <see cref="IAccounts"/>.</param>
/// <param name="upstream">The <see cref="UpstreamLoginOptions"/>.</param>
internal class SignedInAccounts(IAccounts accounts, IOptions<UpstreamLoginOptions> upstream)
{
    /// <summary>
    /// Finds the account signed in to the Identity site for an authorization request.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="request">The <see cref="OpenIddictRequest"/>.</param>
    /// <returns>The <see cref="Account"/>, or <see langword="null"/> when the user must sign in (again).</returns>
    public async Task<Account?> Find(HttpContext context, OpenIddictRequest request)
    {
        var session = await context.AuthenticateAsync(IdentitySchemes.Session);
        if (!session.Succeeded || request.HasPromptValue(PromptValues.Login))
        {
            return null;
        }

        var accountId = SessionPrincipal.AccountId(session.Principal);
        return accountId is null ? null : await accounts.FindById(accountId, context.RequestAborted);
    }

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

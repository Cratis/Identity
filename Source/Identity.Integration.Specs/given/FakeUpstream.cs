// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Encodings.Web;
using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Integration.given;

/// <summary>
/// Replaces GitHub OAuth: a challenge signs the user named in <see cref="UserHeader"/> in immediately, through
/// the same account linking and session principal the real GitHub sign-in uses, and returns to the request.
/// </summary>
/// <param name="options">The options monitor.</param>
/// <param name="logger">The logger factory.</param>
/// <param name="encoder">The URL encoder.</param>
/// <param name="accounts">The <see cref="IAccounts"/>.</param>
public class FakeUpstream(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IAccounts accounts) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string Scheme = "fake-github";
    public const string UserHeader = "x-spec-github-user";
    public const long DefaultUserId = 134365;
    public const string DefaultUser = "134365:einari";

    /// <inheritdoc/>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());

    /// <inheritdoc/>
    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var user = Request.Headers[UserHeader].ToString().Split(':');
        var account = await accounts.RecordGitHubSignIn(new GitHubUser(long.Parse(user[0], CultureInfo.InvariantCulture), user[1], user[1]));
        await Context.SignInAsync(IdentitySchemes.Session, SessionPrincipal.For(account, Scheme), new AuthenticationProperties());
        Response.Redirect(properties.RedirectUri);
    }
}

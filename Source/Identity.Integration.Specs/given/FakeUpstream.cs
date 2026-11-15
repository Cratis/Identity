// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Cratis.Identity.Authorization;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
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
/// <param name="signIn">The production GitHub sign-in events.</param>
internal class FakeUpstream(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    GitHubSignInEvents signIn) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
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
        using var backchannel = new HttpClient(new UserResponse(long.Parse(user[0], CultureInfo.InvariantCulture), user[1]));
        using var tokens = JsonDocument.Parse("{}");
        using var tokenResponse = OAuthTokenResponse.Success(tokens);
        var ticket = new OAuthCreatingTicketContext(
            new ClaimsPrincipal(new ClaimsIdentity()),
            new AuthenticationProperties(),
            Context,
            new AuthenticationScheme(IdentitySchemes.GitHub, IdentitySchemes.GitHub, typeof(OAuthHandler<OAuthOptions>)),
            new OAuthOptions { UserInformationEndpoint = "https://api.github.com/user" },
            backchannel,
            tokenResponse,
            tokens.RootElement);
        await signIn.CreatingTicket(ticket);
        await Context.SignInAsync(IdentitySchemes.Session, ticket.Principal, ticket.Properties);
        Response.Redirect(properties.RedirectUri);
    }

    sealed class UserResponse(long id, string login) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { id, login, name = login }) });
    }
}

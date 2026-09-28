// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;
using System.Text.Json;
using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace Cratis.Identity.Upstream;

/// <summary>
/// Handles the GitHub OAuth sign-in: reads the GitHub user, links it to an account and replaces the upstream
/// principal with the Identity session principal. The GitHub access token is used once and never stored.
/// </summary>
/// <param name="accounts">The <see cref="IAccounts"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public class GitHubSignInEvents(IAccounts accounts, ILogger<GitHubSignInEvents> logger) : OAuthEvents
{
    /// <inheritdoc/>
    public override async Task CreatingTicket(OAuthCreatingTicketContext context)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Cratis-Identity", "1.0"));

        using var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
        response.EnsureSuccessStatusCode();

        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(context.HttpContext.RequestAborted), cancellationToken: context.HttpContext.RequestAborted);
        var user = GitHubUserResponse.Read(body.RootElement) ?? throw new GitHubUserResponseInvalid();

        var account = await accounts.RecordGitHubSignIn(user, context.HttpContext.RequestAborted);
        logger.SignedIn(account.Id, user.Id, user.Login);
        context.Principal = SessionPrincipal.For(account, IdentitySchemes.GitHub);
    }
}

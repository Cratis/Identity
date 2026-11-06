// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;
using System.Text.Json;
using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization;
using Cratis.Identity.Contracts.Accounts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace Cratis.Identity.Upstream;

/// <summary>
/// Handles the GitHub OAuth sign-in: reads the GitHub user, links it to an account and replaces the upstream
/// principal with the Identity session principal. The GitHub access token is used once and never stored.
/// </summary>
/// <param name="accounts">The <see cref="IAccounts"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
/// <param name="eventSourced">The opt-in native command adapter.</param>
/// <param name="timeProvider">The trusted clock.</param>
public class GitHubSignInEvents(IAccounts accounts, ILogger<GitHubSignInEvents> logger, EventSourcedAccountAdapter? eventSourced = null, TimeProvider? timeProvider = null) : OAuthEvents
{
    /// <inheritdoc/>
    public override Task AccessDenied(AccessDeniedContext context)
    {
        context.HandleResponse();
        return Fail(context.HttpContext, context.Properties);
    }

    /// <inheritdoc/>
    public override Task RemoteFailure(RemoteFailureContext context)
    {
        logger.SignInFailed(context.Failure);
        context.HandleResponse();
        return Fail(context.HttpContext, context.Properties);
    }

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

        var account = eventSourced is null
            ? await accounts.RecordGitHubSignIn(user, context.HttpContext.RequestAborted)
            : await eventSourced.SignIn(new GitHubConnection(GitHubConnection.FixedConnectionId, timeProvider ?? TimeProvider.System).From(user, FactId.New()), new(null, user.Name, user.Login));
        logger.SignedIn(account.Id, user.Id, user.Login);
        context.Principal = SessionPrincipal.For(account, IdentitySchemes.GitHub);
    }

    static async Task Fail(HttpContext context, AuthenticationProperties? properties)
    {
        var returnUrl = properties?.RedirectUri;
        var path = $"{context.Request.PathBase}/connect/authorize?";
        if (returnUrl?.StartsWith(path, StringComparison.Ordinal) == true &&
            Uri.TryCreate(returnUrl, UriKind.Relative, out _))
        {
            context.Response.Redirect($"{returnUrl}&upstream_error=access_denied");
            return;
        }

        // No valid OAuth challenge state: never reflect the provider's error or redirect to its return URL.
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync("Sign-in could not be completed. Start the sign-in again.");
    }
}

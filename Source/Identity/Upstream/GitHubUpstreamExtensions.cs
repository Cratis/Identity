// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Upstream;

/// <summary>
/// Extension methods for registering GitHub as upstream login.
/// </summary>
internal static class GitHubUpstreamExtensions
{
    /// <summary>
    /// The path GitHub redirects back to after login. Register it as the OAuth app callback URL.
    /// </summary>
    public const string CallbackPath = "/signin-github";

    /// <summary>
    /// Adds the GitHub OAuth handler as upstream login.
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <returns>The <see cref="AuthenticationBuilder"/> for continuation.</returns>
    public static AuthenticationBuilder AddGitHubUpstream(this AuthenticationBuilder builder)
    {
        builder.Services.AddScoped<GitHubSignInEvents>();
        builder.AddOAuth(IdentitySchemes.GitHub, "GitHub", _ => { });
        builder.Services.AddOptions<OAuthOptions>(IdentitySchemes.GitHub).Configure<IOptions<IdentityOptions>>((options, identity) =>
        {
            options.ClientId = identity.Value.GitHub.ClientId;
            options.ClientSecret = identity.Value.GitHub.ClientSecret;
            options.CallbackPath = CallbackPath;
            options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
            options.TokenEndpoint = "https://github.com/login/oauth/access_token";
            options.UserInformationEndpoint = "https://api.github.com/user";
            options.UsePkce = true;
            options.SaveTokens = false;
            options.SignInScheme = IdentitySchemes.Session;
            options.EventsType = typeof(GitHubSignInEvents);

            // The public profile is all Identity needs; no GitHub scopes are requested.
            options.Scope.Clear();
        });

        return builder;
    }
}

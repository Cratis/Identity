// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Identity.Replay.for_EventLog.given;

internal static class UpstreamTicket
{
    internal static async Task SignIn(IAccountSignIn accounts, TimeProvider clock)
    {
        var properties = new AuthenticationProperties();
        properties.StoreTokens([
            new() { Name = "access_token", Value = Secrets.Access }, new() { Name = "refresh_token", Value = Secrets.Refresh },
            new() { Name = "authorization_code", Value = Secrets.Code }, new() { Name = "client_secret", Value = Secrets.Client }
        ]);
        using var tokens = JsonDocument.Parse(JsonSerializer.Serialize(new { access_token = Secrets.Access, refresh_token = Secrets.Refresh, id_token = Secrets.Jwt }));
        using var user = JsonDocument.Parse("{}");
        using var response = OAuthTokenResponse.Success(tokens);
        using var client = new HttpClient(new UserResponse());
        var options = new OAuthOptions { UserInformationEndpoint = "https://api.github.com/user", ClientSecret = Secrets.Client };
        var context = new OAuthCreatingTicketContext(new ClaimsPrincipal(new ClaimsIdentity()), properties, new DefaultHttpContext(), new AuthenticationScheme("GitHub", "GitHub", typeof(OAuthHandler<OAuthOptions>)), options, client, response, user.RootElement);
        await new GitHubSignInEvents(accounts, NullLogger<GitHubSignInEvents>.Instance, clock).CreatingTicket(context);
    }

    sealed class UserResponse : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(/*lang=json*/ """{ "id": 42, "login": "synthetic-login", "name": "Person", "email": "profile@example.test" }""") });
    }
}

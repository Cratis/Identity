// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Identity.Accounts;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Identity.for_GitHubSignInEvents;

public class when_the_sign_in_command_is_refused : Specification
{
    OAuthCreatingTicketContext _context;
    ClaimsPrincipal _initial;
    Exception _error;

    async Task Because()
    {
        var signIn = Substitute.For<IAccountSignIn>();
        signIn.SignIn(Arg.Any<VerifiedAuthentication>(), Arg.Any<ObservedProfile>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<Account>(new AccountOperationRefused("account_not_active")));
        using var http = new HttpClient(new ProfileHandler());
        using var token = OAuthTokenResponse.Success(JsonDocument.Parse("{\"access_token\":\"synthetic-test-token\"}"));
        using var user = JsonDocument.Parse("{}");
        _initial = new(new ClaimsIdentity());
        _context = new(_initial, new(), new DefaultHttpContext(), new("GitHub", "GitHub", typeof(OAuthHandler<OAuthOptions>)), new() { UserInformationEndpoint = "https://api.example.test/user" }, http, token, user.RootElement);
        try
        {
            await new GitHubSignInEvents(signIn, NullLogger<GitHubSignInEvents>.Instance, TimeProvider.System).CreatingTicket(_context);
        }
        catch (AccountOperationRefused error)
        {
            _error = error;
        }
    }

    [Fact] void should_refuse_the_ticket() => _error.ShouldBeOfExactType<AccountOperationRefused>();
    [Fact] void should_not_replace_the_principal() => ReferenceEquals(_initial, _context.Principal).ShouldBeTrue();
    [Fact] void should_not_issue_a_session_cookie() => _context.Response.Headers.ContainsKey("Set-Cookie").ShouldBeFalse();

    sealed class ProfileHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { id = 42, login = "display", name = "Person" }) });
    }
}

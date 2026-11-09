// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Identity.Accounts;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Identity.for_GitHubSignInEvents;

public class when_the_request_is_cancelled_after_the_account_is_signed_in : Specification
{
    OAuthCreatingTicketContext _context;
    ClaimsPrincipal _initial;
    CancellationTokenSource _aborted;
    Exception _error;

    async Task Because()
    {
        _aborted = new();
        var signIn = Substitute.For<IAccountSignIn>();
        signIn.SignIn(Arg.Any<VerifiedAuthentication>(), Arg.Any<ObservedProfile>(), Arg.Any<CancellationToken>()).Returns<Task<Account>>(_ => throw new OperationCanceledException(_aborted.Token));
        using var http = new HttpClient(new ProfileHandler());
        using var token = OAuthTokenResponse.Success(JsonDocument.Parse("{\"access_token\":\"synthetic-test-token\"}"));
        using var user = JsonDocument.Parse("{}");
        _initial = new(new ClaimsIdentity());
        _context = new(_initial, new(), new DefaultHttpContext { RequestAborted = _aborted.Token }, new("GitHub", "GitHub", typeof(OAuthHandler<OAuthOptions>)), new() { UserInformationEndpoint = "https://api.example.test/user" }, http, token, user.RootElement);
        _error = await Catch.Exception(() => new GitHubSignInEvents(signIn, NullLogger<GitHubSignInEvents>.Instance, TimeProvider.System).CreatingTicket(_context));
    }

    void Destroy() => _aborted.Dispose();

    [Fact] void should_not_swallow_the_cancellation_as_a_refusal() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_replace_the_principal_with_a_session() => _context.Principal.ShouldEqual(_initial);

    sealed class ProfileHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { id = 42, login = "synthetic-login", name = "Synthetic User", email = (string?)null }) });
    }
}

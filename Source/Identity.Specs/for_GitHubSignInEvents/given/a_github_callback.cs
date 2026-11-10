// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Identity.for_GitHubSignInEvents.given;

public class a_github_callback : Specification
{
    private protected ObservedProfile _profile;
    protected OAuthCreatingTicketContext _context;
    private protected Account _account;

    protected async Task CreateTicket([StringSyntax(StringSyntaxAttribute.Json)] string response)
    {
        _account = new(AccountSubject.New(), "Synthetic User", null, "42", "synthetic-login", AccountStatus.Active, []);
        var signIn = Substitute.For<IAccountSignIn>();
        signIn.SignIn(Arg.Any<VerifiedAuthentication>(), Arg.Do<ObservedProfile>(profile => _profile = profile), Arg.Any<CancellationToken>()).Returns(_account);
        using var http = new HttpClient(new ProfileHandler(response));
        using var token = OAuthTokenResponse.Success(JsonDocument.Parse("{\"access_token\":\"synthetic-test-token\"}"));
        using var user = JsonDocument.Parse("{}");
        _context = new(new ClaimsPrincipal(new ClaimsIdentity()), new(), new DefaultHttpContext(), new("GitHub", "GitHub", typeof(OAuthHandler<OAuthOptions>)), new() { UserInformationEndpoint = "https://api.example.test/user" }, http, token, user.RootElement);
        var services = new ServiceCollection().AddSingleton(signIn).BuildServiceProvider();
        await new GitHubSignInEvents(services, NullLogger<GitHubSignInEvents>.Instance, TimeProvider.System).CreatingTicket(_context);
    }

    sealed class ProfileHandler(string response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response) });
    }
}

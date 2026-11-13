// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cratis.Identity.for_GitHubSignInEvents;

public class when_logging_a_successful_sign_in : Specification
{
    const string GitHubId = "987654321";
    const string GitHubLogin = "synthetic-private-login";
    CapturingLogger _logger;
    IAccountSignIn _accounts;
    Account _account;

    void Establish()
    {
        _logger = new();
        _accounts = Substitute.For<IAccountSignIn>();
        _account = new(AccountSubject.New(), "Synthetic User", null, GitHubId, GitHubLogin, AccountStatus.Active, []);
        _accounts.SignIn(Arg.Any<VerifiedAuthentication>(), Arg.Any<ObservedProfile>(), Arg.Any<CancellationToken>()).Returns(_account);
    }

    async Task Because()
    {
        using var http = new HttpClient(new ProfileHandler());
        using var token = OAuthTokenResponse.Success(JsonDocument.Parse("""{ "access_token": "synthetic-test-token" }"""));
        using var user = JsonDocument.Parse("{}");
        var context = new OAuthCreatingTicketContext(new ClaimsPrincipal(new ClaimsIdentity()), new(), new DefaultHttpContext(), new("GitHub", "GitHub", typeof(OAuthHandler<OAuthOptions>)), new() { UserInformationEndpoint = "https://api.example.test/user" }, http, token, user.RootElement);
        await new GitHubSignInEvents(_accounts, _logger, TimeProvider.System).CreatingTicket(context);
    }

    [Fact] void should_log_only_the_account_reference() => _logger.Message.ShouldEqual($"Account {_account.Id.Value} signed in with GitHub");
    [Fact] void should_not_log_the_upstream_subject() => _logger.Message.Contains(GitHubId, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_log_the_upstream_login() => _logger.Message.Contains(GitHubLogin, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_attach_the_upstream_subject_as_a_structured_property() => _logger.Properties.Any(property => property.Key == "GitHubId" || property.Value?.ToString() == GitHubId).ShouldBeFalse();
    [Fact] void should_not_attach_the_upstream_login_as_a_structured_property() => _logger.Properties.Any(property => property.Key == "GitHubLogin" || property.Value?.ToString() == GitHubLogin).ShouldBeFalse();

    sealed class CapturingLogger : ILogger<GitHubSignInEvents>
    {
        public string Message { get; private set; } = string.Empty;
        public IReadOnlyList<KeyValuePair<string, object?>> Properties { get; private set; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Message = formatter(state, exception);
            Properties = ((IEnumerable<KeyValuePair<string, object?>>)state).ToArray();
        }
    }

    sealed class ProfileHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent($$"""{ "id": {{GitHubId}}, "login": "{{GitHubLogin}}", "name": "Synthetic User" }""") });
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cratis.Identity.for_GitHubSignInEvents;

public class when_the_remote_sign_in_fails : Specification
{
    const string ReturnUrl = "/connect/authorize?client_id=cratis-cli";

    CapturingLogger _logger;
    GitHubSignInEvents _events;
    RemoteFailureContext _context;
    Exception _failure;

    void Establish()
    {
        _logger = new CapturingLogger();
        _events = new GitHubSignInEvents(Substitute.For<IAccounts>(), _logger);
        _failure = new AuthenticationFailureException("The oauth state was missing or invalid.");
        _context = new RemoteFailureContext(
            new DefaultHttpContext(),
            new AuthenticationScheme("GitHub", "GitHub", typeof(OAuthHandler<OAuthOptions>)),
            new OAuthOptions(),
            _failure)
        {
            Properties = new AuthenticationProperties { RedirectUri = ReturnUrl }
        };
    }

    Task Because() => _events.RemoteFailure(_context);

    [Fact] void should_log_the_failure() => _logger.Exceptions.ShouldContainOnly(_failure);
    [Fact] void should_log_it_as_a_warning() => _logger.Levels.ShouldContainOnly(LogLevel.Warning);
    [Fact] void should_redirect_back_to_the_authorization_request() => _context.HttpContext.Response.Headers.Location.ToString().ShouldEqual($"{ReturnUrl}&upstream_error=access_denied");

    sealed class CapturingLogger : ILogger<GitHubSignInEvents>
    {
        public List<Exception?> Exceptions { get; } = [];
        public List<LogLevel> Levels { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Levels.Add(logLevel);
            Exceptions.Add(exception);
        }
    }
}

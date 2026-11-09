// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.RateLimiting;
using Cratis.Identity.Configuration;
using Cratis.Identity.given;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_TokenRequestRateLimiter;

public class when_refusals_are_logged : Specification
{
    readonly List<string> _messages = [];
    FixedTimeProvider _time;
    TokenRequestRateLimiter _limiter;
    int _firstWindow;

    void Establish()
    {
        _time = new(DateTimeOffset.UtcNow);
        _limiter = new(_time, Options.Create(new IdentityOptions { TokenRateLimit = new() { PerClientRequests = 1, PerSourceRequests = 1000 } }), new CapturingLogger(_messages));
    }

    void Because()
    {
        _limiter.Acquire("private-client-id", null, "source");
        for (var index = 0; index < 5; index++)
        {
            _limiter.Acquire("private-client-id", null, "source");
        }

        _firstWindow = _messages.Count;
        _time.Now += TimeSpan.FromMinutes(2);
        _limiter.Acquire("private-client-id", null, "source");
        _limiter.Acquire("private-client-id", null, "source");
    }

    [Fact] void should_log_once_per_reason_per_window() => _firstWindow.ShouldEqual(1);
    [Fact] void should_log_again_after_the_window() => _messages.Count.ShouldEqual(2);
    [Fact] void should_never_log_the_raw_client_id() => _messages.Exists(_ => _.Contains("private-client-id", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_report_suppressed_refusals() => _messages[1].ShouldContain("suppressed 4");

    sealed class CapturingLogger(List<string> messages) : ILogger<TokenRequestRateLimiter>
    {
        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            messages.Add(formatter(state, exception));

        sealed class NullScope : IDisposable
        {
            internal static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}

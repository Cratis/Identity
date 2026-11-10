// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Identity.RevocationSpecs;

public class when_a_recovery_pass_fails_in_the_worker : Specification
{
    ILogger<RevocationOutboxWorker> _logger;
    RevocationOutboxWorker _worker;

    void Establish()
    {
        _logger = Substitute.For<ILogger<RevocationOutboxWorker>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var scopes = Substitute.For<IServiceScopeFactory>();
        scopes.CreateScope().Returns(_ => throw new RevocationPending());
        _worker = new(scopes, _logger, new RecoveryGate());
    }

    async Task Because()
    {
        await _worker.StartAsync(CancellationToken.None);
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        await _worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    void should_log_the_recovery_failure_exactly_once() => _logger.ReceivedCalls().Count(call =>
        call.GetMethodInfo().Name == nameof(ILogger.Log) &&
        Equals(call.GetArguments()[0], LogLevel.Warning) &&
        call.GetArguments()[1] is EventId { Id: 3 }).ShouldEqual(1);
}

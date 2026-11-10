// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using Microsoft.Extensions.Logging;

namespace Cratis.Identity.RevocationSpecs;

public class when_recovery_closes_admission_after_failure : given.a_recovery_pass
{
    ILogger<ReconcileRecovery> _logger;

    void Establish()
    {
        _logger = Substitute.For<ILogger<ReconcileRecovery>>();
        _logger.IsEnabled(LogLevel.Warning).Returns(true);
        _allocated = 2;
        _recovery = new(_log, _operational, _authority, new ReconcileReservation(_log, _authority), _facts, _dispatch, _gate, _session, _options, TimeProvider.System, _logger);
    }

    async Task Because() => await Run();

    [Fact] void should_keep_admission_closed() => _gate.Ready.ShouldBeFalse();
    [Fact] void should_preserve_the_failure() => _failure.ShouldBeOfExactType<RevocationPending>();
    [Fact]
    void should_log_the_exception_at_warning_level() => _logger.ReceivedCalls().ShouldContain(call =>
        call.GetMethodInfo().Name == nameof(ILogger.Log) &&
        Equals(call.GetArguments()[0], LogLevel.Warning) &&
        ReferenceEquals(call.GetArguments()[3], _failure));
    [Fact]
    void should_include_the_failure_type_and_message() => _logger.ReceivedCalls().ShouldContain(call =>
        call.GetMethodInfo().Name == nameof(ILogger.Log) &&
        call.GetArguments()[2].ToString().Contains(typeof(RevocationPending).FullName) &&
        call.GetArguments()[2].ToString().Contains(_failure.Message));
}

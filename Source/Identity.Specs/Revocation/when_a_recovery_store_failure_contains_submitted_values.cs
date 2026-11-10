// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using Microsoft.Extensions.Logging;

namespace Cratis.Identity.RevocationSpecs;

public class when_a_recovery_store_failure_contains_submitted_values : Specification
{
    ILogger _logger;
    object?[] _arguments;

    void Establish()
    {
        _logger = Substitute.For<ILogger>();
        _logger.IsEnabled(LogLevel.Warning).Returns(true);
    }

    void Because()
    {
        RecoveryFailureDiagnostics.Warn(_logger, new InvalidOperationException("synthetic-submitted-value"));
        _arguments = _logger.ReceivedCalls().Single(call => call.GetMethodInfo().Name == nameof(ILogger.Log)).GetArguments();
    }

    [Fact] void should_warn() => _arguments[0].ShouldEqual(LogLevel.Warning);
    [Fact] void should_include_the_exception_type() => _arguments[2].ToString().ShouldContain(typeof(InvalidOperationException).FullName);
    [Fact] void should_not_include_the_submitted_value() => _arguments[2].ToString().ShouldNotContain("synthetic-submitted-value");
    [Fact] void should_not_attach_the_untrusted_exception() => _arguments[3].ShouldBeNull();
}

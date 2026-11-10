// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>Recovery failures without store payloads, connection strings, or submitted values.</summary>
internal static class RecoveryFailureDiagnostics
{
    static readonly Action<ILogger, string, string, Exception?> _failure = LoggerMessage.Define<string, string>(
        LogLevel.Warning,
        new EventId(3, "RestrictionRecoveryFailed"),
        "Identity restriction recovery failed ({FailureType}): {FailureMessage}");

    /// <summary>Logs a failure type and only messages known to contain no submitted values.</summary>
    /// <param name="logger">The diagnostic sink.</param>
    /// <param name="exception">The recovery failure.</param>
    internal static void Warn(ILogger logger, Exception exception)
    {
        // Only these sealed domain exceptions have fixed, payload-free messages.
        var safe = exception is RevocationPending or FenceLogConflict or OutboxContentConflict;
        _failure(
            logger,
            exception.GetType().FullName!,
            safe ? exception.Message : "Store or transport details withheld to protect submitted values.",
            safe ? exception : null);
    }
}

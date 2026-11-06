// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Identity.Storage;

/// <summary>Reports append failures without trusting kernel exception text or event content.</summary>
internal static partial class ChronicleAppendFailure
{
    /// <summary>Logs safe append metadata and creates a distinct infrastructure failure.</summary>
    /// <param name="result">The unsuccessful kernel result.</param>
    /// <param name="eventSourceId">The attempted source identity.</param>
    /// <param name="eventTypes">The attempted event types, never event content.</param>
    /// <param name="logger">The boundary logger.</param>
    /// <returns>An exception containing only safe diagnostics.</returns>
    internal static InvalidOperationException Create(AppendManyResult result, string eventSourceId, IEnumerable<EventType> eventTypes, ILogger logger)
    {
        var source = SafeId(eventSourceId);
        var types = string.Join(", ", eventTypes.Concat(result.EventTypes).Select(type => SafeId(type.Id.Value)).Distinct());
        var errors = string.Join("; ", result.Errors.Select(error => SafeError(error.Value)));
        var violations = string.Join("; ", result.ConcurrencyViolations.Select(violation =>
            $"event source id: {SafeId(violation.EventSourceId.Value)}, event type ids: {string.Join(", ", (violation.Scope?.EventTypes ?? []).Select(type => SafeId(type.Id.Value)).Append(SafeId(violation.ConflictingEventTypeId?.Value)).Distinct())}, expected sequence: {violation.ExpectedEventSequenceNumber.Value}, actual sequence: {violation.ActualEventSequenceNumber.Value}"));
        if (violations.Length == 0)
        {
            violations = $"concurrency: none, constraints: {result.ConstraintViolations.Count()}, expected sequence: not reported, actual sequence: not reported";
        }

        AppendFailed(logger, source, types, errors, violations);
        return new InvalidOperationException($"Chronicle append failed; event source id: {source}; event type ids: {types}; kernel errors: {errors}; violations: {violations}");
    }

    /// <summary>Preserves only fixed kernel codes: v19.111.0 EventSequence.BatchValidation also returns arbitrary ex.Message text that may echo payloads.</summary>
    /// <param name="message">The untrusted kernel error.</param>
    /// <returns>An allowlisted code or a redaction marker.</returns>
    static string SafeError(string message) => message switch
    {
        "PrivateEventTypeCannotBeAppendedToOutbox" or
        "PublicEventTypeCannotBeAppendedToEventLog" or
        "EventPublicationConflict" or
        "ConstraintIndexUnavailable" or
        "AppendOutcomeUnknown" or
        "EffectReceiptUnsettled" => message,
        "" => "Unknown",
        _ => "[redacted kernel error]"
    };

    static string SafeId(string? value) => value is null ? "not reported" :
        Guid.TryParseExact(value, "D", out var id) ? id.ToString("D") : "[redacted identifier]";

    [LoggerMessage(LogLevel.Error, "Chronicle append failed; event source id: {EventSourceId}; event type ids: {EventTypeIds}; kernel errors: {KernelErrors}; violations: {Violations}")]
    static partial void AppendFailed(ILogger logger, string eventSourceId, string eventTypeIds, string kernelErrors, string violations);
}

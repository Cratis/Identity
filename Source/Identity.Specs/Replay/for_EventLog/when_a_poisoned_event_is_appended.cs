// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Testing.Events;
using Cratis.Identity.Replay.given;

namespace Cratis.Identity.Replay.for_EventLog;

public class when_a_poisoned_event_is_appended : Specification
{
    readonly EventStoreForTesting _store = new();
    IReadOnlyList<(string EventType, string Rule)> _findings;

    async Task Because()
    {
        await _store.EventLog.Append(Guid.NewGuid(), new given.PoisonedEvent(given.Secrets.Jwt));
        var events = await PublicationReplay.Read(_store.EventLog);
        _findings = given.EventLogScanner.Findings(events.Select(@event => JsonSerializer.Serialize(new { eventType = "poisoned", content = @event.Content, context = @event.Context })), given.Secrets.All);
    }

    [Fact] void should_fail_the_scan_with_redacted_diagnostics() => _findings.ToArray().ShouldEqual(new[] { ("poisoned", "jwt"), ("poisoned", "known-secret") });
    void Destroy() => _store.Dispose();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Contracts.Clients;

namespace Cratis.Identity.Clients.Registration;

/// <summary>The guarded client fact boundary.</summary>
/// <param name="eventStore">The Chronicle store.</param>
/// <param name="readModels">The passive state reader.</param>
/// <param name="time">The trusted append clock.</param>
public class ClientRegistrationStore(IEventStore eventStore, IReadModels readModels, TimeProvider time)
{
    /// <summary>Reads a complete passive client state with its raw-source guard.</summary>
    /// <param name="id">The opaque registration identity.</param>
    /// <returns>The detached decision token and passive state.</returns>
    internal virtual async Task<GuardedClientRead> Read(ClientRegistrationId id)
    {
        var key = new ReadModelKey(id.Value.ToString("D"));
        var guard = await eventStore.GetDecisionReads().GetDetached<ClientGuard>(key);
        var state = await readModels.GetInstanceById<ClientRegistrationState>(key) ?? new(id.Value.ToString("D"));
        return new(guard, state);
    }

    /// <summary>Appends only when the stream still matches the decision read.</summary>
    /// <param name="id">The opaque registration identity.</param>
    /// <param name="events">The internal source facts.</param>
    /// <param name="read">The guarded decision.</param>
    /// <returns>The append outcome.</returns>
    internal virtual async Task<ClientRegistrationOutcome> Append(ClientRegistrationId id, IEnumerable<object> events, GuardedClientRead read)
    {
        var result = await eventStore.EventLog.AppendMany(events.Select(@event => new EventForEventSourceId(id.Value.ToString("D"), @event) { Occurred = time.GetUtcNow() }), [read.Guard]);
        if (result.IsSuccess)
        {
            return ClientRegistrationOutcome.Success;
        }

        return result.HasConstraintViolations ? ClientRegistrationOutcome.AlreadyRegistered : ClientRegistrationOutcome.Conflict;
    }

    /// <summary>The guarded decision boundary for a client stream.</summary>
    /// <param name="Guard">The raw-source decision token.</param>
    /// <param name="State">The passive state used by the decision.</param>
    internal record GuardedClientRead(DecisionRead<ClientGuard> Guard, ClientRegistrationState State);
}

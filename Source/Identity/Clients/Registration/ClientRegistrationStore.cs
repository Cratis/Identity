// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Identity.Clients.Registration;

/// <summary>The guarded client fact boundary.</summary>
/// <param name="eventStore">The Chronicle store.</param>
/// <param name="readModels">The passive state reader.</param>
/// <param name="time">The trusted append clock.</param>
/// <param name="logger">The optional append failure logger.</param>
public class ClientRegistrationStore(IEventStore eventStore, IReadModels readModels, TimeProvider time, ILogger<ClientRegistrationStore>? logger = null)
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
    /// <exception cref="InvalidOperationException">Chronicle reported an append failure other than a constraint or concurrency violation.</exception>
    internal virtual async Task<ClientRegistrationOutcome> Append(ClientRegistrationId id, IEnumerable<object> events, GuardedClientRead read)
    {
        var batch = events.ToArray();
        var result = await AppendGuarded(batch.Select(@event => new EventForEventSourceId(id.Value.ToString("D"), @event) { Occurred = time.GetUtcNow() }), read.Guard);
        if (result.IsSuccess)
        {
            return ClientRegistrationOutcome.Success;
        }

        if (!result.HasErrors)
        {
            if (result.HasConstraintViolations)
            {
                return ClientRegistrationOutcome.AlreadyRegistered;
            }

            if (result.HasConcurrencyViolations)
            {
                return ClientRegistrationOutcome.Conflict;
            }
        }

        throw ChronicleAppendFailure.Create(result, id.Value.ToString("D"), batch.Select(@event => eventStore.EventTypes.GetEventTypeFor(@event.GetType())), logger ?? NullLogger<ClientRegistrationStore>.Instance);
    }

    /// <summary>Sends the guarded batch to Chronicle.</summary>
    /// <param name="events">The source facts.</param>
    /// <param name="guard">The detached decision token.</param>
    /// <returns>The unclassified kernel result.</returns>
    internal virtual Task<AppendManyResult> AppendGuarded(IEnumerable<EventForEventSourceId> events, DecisionRead<ClientGuard> guard) =>
        eventStore.EventLog.AppendMany(events, [guard]);

    /// <summary>The guarded decision boundary for a client stream.</summary>
    /// <param name="Guard">The raw-source decision token.</param>
    /// <param name="State">The passive state used by the decision.</param>
    internal record GuardedClientRead(DecisionRead<ClientGuard> Guard, ClientRegistrationState State);
}

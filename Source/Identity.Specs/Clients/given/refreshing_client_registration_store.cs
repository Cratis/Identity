// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Clients.Registration;
using Cratis.Identity.Contracts.Clients;

namespace Cratis.Identity.Clients.given;

public class refreshing_client_registration_store(IEventStore events, IReadModels reader, TimeProvider time, Func<ClientRegistrationId, Task> refresh)
    : ClientRegistrationStore(events, reader, time)
{
    internal Queue<ClientRegistrationOutcome> Outcomes { get; } = new();
    internal List<object> Appended { get; } = [];
    internal Func<Task>? OnRejected { get; set; }
    internal int Attempts { get; private set; }

    internal override async Task<GuardedClientRead> Read(ClientRegistrationId id)
    {
        await refresh(id);
        return await base.Read(id);
    }

    internal override async Task<ClientRegistrationOutcome> Append(ClientRegistrationId id, IEnumerable<object> events, GuardedClientRead read)
    {
        Attempts++;
        if (Outcomes.TryDequeue(out var outcome))
        {
            if (OnRejected is not null)
            {
                await OnRejected();
            }

            return outcome;
        }

        var batch = events.ToArray();
        var result = await base.Append(id, batch, read);
        if (result == ClientRegistrationOutcome.Success)
        {
            Appended.AddRange(batch);
        }

        return result;
    }
}

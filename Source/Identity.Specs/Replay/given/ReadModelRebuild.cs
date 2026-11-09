// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Identity.Accounts;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Replay.for_EventLog.given;

namespace Cratis.Identity.Replay.given;

internal sealed class ReadModelRebuild
{
    internal readonly Guid AccountId = Guid.NewGuid();
    internal readonly Guid OtherAccountId = Guid.NewGuid();
    readonly object[] _events = new[]
    {
        typeof(AccountRegistrationRecorded), typeof(AccountNameChanged), typeof(AccountEmailChanged),
        typeof(UpstreamKeyLinkRecorded), typeof(UpstreamKeySubjectRecorded), typeof(UpstreamKeyDisplayIdentifierObserved),
        typeof(AccountDisablementRecorded), typeof(AccountEnablementRecorded), typeof(UpstreamKeyUnlinkRecorded), typeof(AccountTombstoneRecorded)
    }.Select(EventSeeder.Seed).ToArray();

    internal async Task<string[]> Rebuild() =>
    [
        JsonSerializer.Serialize(await Build<Account>()), JsonSerializer.Serialize(await Build<AccountGuard>()),
        JsonSerializer.Serialize(await Build<UpstreamKeyLookup>()), JsonSerializer.Serialize(Reduce()),
        JsonSerializer.Serialize(await Build<Account>(true)), JsonSerializer.Serialize(await Build<AccountGuard>(true)),
        JsonSerializer.Serialize(await Build<UpstreamKeyLookup>(true)), JsonSerializer.Serialize(Reduce(true))
    ];

    internal AccountLinkState Reduce(bool other = false)
    {
        var reducer = new AccountLinkStateReducer();
        AccountLinkState? state = null;
        var context = EventContext.EmptyWithEventSourceId(other ? OtherAccountId : AccountId) with { Occurred = DateTimeOffset.UnixEpoch };
        foreach (var @event in other ? _events.Take(4) : _events)
        {
            var method = typeof(AccountLinkStateReducer).GetMethod("On", [@event.GetType(), typeof(AccountLinkState), typeof(EventContext)]);
            if (method is not null) state = (AccountLinkState)method.Invoke(reducer, [@event, state, context])!;
        }

        return state!;
    }

    internal async Task<T> Build<T>(bool other = false)
        where T : class
    {
        using var scenario = new ReadModelScenario<T>();
        await scenario.Given.ForEventSource(AccountId).Events(_events);
        await scenario.Given.ForEventSource(OtherAccountId).Events(_events.Take(4).ToArray());
        return scenario.InstanceForEventSourceId(other ? OtherAccountId : AccountId)!;
    }
}

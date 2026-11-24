// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.Events;
using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Replay.for_EventLog.given;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Identity.Replay.given;

internal sealed class PublicationReplay : IDisposable
{
    internal readonly EventStoreForTesting Store = new();
    internal readonly EventSourceId Account = Guid.NewGuid();
    internal readonly object[] Facts = new[]
    {
        typeof(AccountRegistrationRecorded), typeof(UpstreamKeyLinkRecorded), typeof(AccountDisablementRecorded),
        typeof(AccountEnablementRecorded), typeof(UpstreamKeyUnlinkRecorded), typeof(AccountTombstoneRecorded), typeof(AccountSignInRecorded)
    }.Select(EventSeeder.Seed).ToArray();

    internal async Task Replay(ulong firstSequence, IEventStore? store = null)
    {
        var publisher = new AccountPublications(store ?? Store, NullLogger<AccountPublications>.Instance);
        foreach (var fact in Facts)
        {
            var context = EventContext.EmptyWithEventSourceId(Account) with { SequenceNumber = firstSequence++, Occurred = DateTimeOffset.UnixEpoch };
            await Publish(publisher, fact, context);
        }
    }

    internal Task<AppendedEvent[]> Published() => Read(Store.GetEventSequence(EventSequenceId.Outbox));

    internal static Task Publish(AccountPublications publisher, object fact, EventContext context) =>
        (Task)typeof(AccountPublications).GetMethod(nameof(AccountPublications.Publish), [fact.GetType(), typeof(EventContext)])!.Invoke(publisher, [fact, context])!;

    internal static async Task<AppendedEvent[]> Read(IEventSequence sequence) => (await sequence.GetFromSequenceNumber(EventSequenceNumber.First)).ToArray();

    public void Dispose() => Store.Dispose();
}

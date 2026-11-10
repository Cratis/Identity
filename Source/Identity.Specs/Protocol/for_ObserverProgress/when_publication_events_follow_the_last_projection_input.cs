// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Configuration;
using Cratis.Identity.Contracts.Accounts.Events;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Protocol.for_ObserverProgress;

public class when_publication_events_follow_the_last_projection_input : Specification
{
    IEventLog _log;
    EventSequenceNumber _tail;
    EventType[] _requested;
    ObserverProgress _progress;

    void Establish()
    {
        var store = Substitute.For<IEventStore>();
        _log = Substitute.For<IEventLog>();
        store.EventLog.Returns(_log);
        store.EventTypes.GetEventTypeFor(Arg.Any<Type>()).Returns(new EventType("UpstreamKeyLinkRecorded", 1));
        store.Projections.GetStateForModel(Arg.Any<Type>()).Returns(new ProjectionState(ObserverRunningState.Active, true, new(6), new(5), new(99)));
        store.Projections.GetFailedPartitionsForModel(Arg.Any<Type>()).Returns(Task.FromResult<IEnumerable<FailedPartition>>([]));
        store.EventTypes.GetEventTypeFor(typeof(UpstreamKeyLinkRecorded)).Returns(new EventType("UpstreamKeyLinkRecorded", 1));
        store.EventTypes.GetEventTypeFor(typeof(UpstreamKeyUnlinkRecorded)).Returns(new EventType("UpstreamKeyUnlinkRecorded", 1));
        _log.GetTailSequenceNumber(filterEventTypes: Arg.Any<IEnumerable<EventType>>()).Returns(call =>
        {
            _requested = call.ArgAt<IEnumerable<EventType>>(4).ToArray();
            return Task.FromResult(new EventSequenceNumber(_requested.Length == 0 ? 99UL : 5UL));
        });
        _progress = new ObserverProgress(store);
    }

    async Task Because()
    {
        _tail = await _progress.Tail(typeof(UpstreamKeyLookup));
        var catchUp = new ReadModelCatchUp(_progress, Options.Create(new IdentityOptions()), TimeProvider.System);
        await catchUp.BeforeRetry(CancellationToken.None);
    }

    [Fact] void should_not_target_the_trailing_publication() => _tail.ShouldEqual(new EventSequenceNumber(5));
    [Fact] void should_only_filter_to_projection_inputs() => _requested.Length.ShouldEqual(2);
    [Fact] Task should_never_read_the_unfiltered_tail() => _log.DidNotReceive().GetTailSequenceNumber(filterEventTypes: null);
}

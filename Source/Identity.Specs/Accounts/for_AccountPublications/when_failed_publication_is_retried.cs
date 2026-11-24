// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Identity.Replay.given;

namespace Cratis.Identity.Accounts.for_AccountPublications;

public class when_failed_publication_is_retried : Specification
{
    readonly PublicationReplay _replay = new();
    readonly InvalidOperationException _unavailable = new("outbox unavailable");
    Exception? _failure;
    int _before;
    int _after;

    async Task Because()
    {
        var failed = Substitute.For<IEventStore>();
        failed.EventTypes.Returns(_replay.Store.EventTypes);
        failed.GetEventSequence(EventSequenceId.Outbox).Returns(_ => throw _unavailable);
        _failure = await Catch.Exception(() => _replay.Replay(1, failed));
        _before = (await _replay.Published()).Length;
        await _replay.Replay(1000);
        await _replay.Replay(2000);
        _after = (await _replay.Published()).Length;
    }

    [Fact] void should_hold_the_partition_by_rethrowing_the_failure() => _failure.ShouldEqual(_unavailable);
    [Fact] void should_append_nothing_on_failure() => _before.ShouldEqual(0);
    [Fact] void should_publish_exactly_once_when_retried() => _after.ShouldEqual(7);
    void Destroy() => _replay.Dispose();
}

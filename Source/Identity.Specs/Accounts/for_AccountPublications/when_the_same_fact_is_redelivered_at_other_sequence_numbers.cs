// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Identity.Replay.given;

namespace Cratis.Identity.Accounts.for_AccountPublications;

public class when_the_same_fact_is_redelivered_at_other_sequence_numbers : Specification
{
    readonly PublicationReplay _replay = new();
    AppendedEvent[] _first;
    AppendedEvent[] _second;

    async Task Because()
    {
        await _replay.Replay(1);
        _first = await _replay.Published();
        await _replay.Replay(1000);
        _second = await _replay.Published();
    }

    [Fact] void should_publish_each_of_the_seven_contracts_once() => _second.GroupBy(@event => @event.Content.GetType()).Select(group => group.Count()).ShouldEqual([1, 1, 1, 1, 1, 1, 1]);
    [Fact] void should_keep_the_original_identity_tags() => _second.Select(@event => @event.Context.Tags.Single().Value).ShouldEqual(_first.Select(@event => @event.Context.Tags.Single().Value));
    [Fact] void should_keep_the_full_ordered_log_after_a_second_replay() => _second.Select(@event => @event.Content.GetType()).ShouldEqual(_first.Select(@event => @event.Content.GetType()));
    void Destroy() => _replay.Dispose();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_chronicle_reports_conflicting_content : Specification
{
    Exception? _error;
    AppendResult _result;

    async Task Because()
    {
        var sequence = KeyedSequenceProxy.Create(out var proxy);
        proxy.Supported = true;
        _result = new AppendResult { IdempotencyConflicts = [new(new IdempotencyKey("conflicting-key"), default!, default!, default!, default)] };
        proxy.Result = _result;
        var store = Substitute.For<IEventStore>();
        store.GetEventSequence(EventSequenceId.Log).Returns(sequence);
        var entry = OutboxEntry.Create(FactKind.FamilyStarted, "grant", FenceDoubles.Subject, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(30));
        _error = await Catch.Exception(() => new ChronicleRevocationFactWriter(store).Append(entry, CancellationToken.None));
    }
    [Fact] void should_demonstrate_the_pinned_clients_success_flag_does_not_cover_conflicts() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_hold_the_intent_instead_of_marking_it_delivered() => _error.ShouldBeOfExactType<OutboxContentConflict>();
}

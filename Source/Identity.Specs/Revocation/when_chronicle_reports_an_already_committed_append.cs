// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_chronicle_reports_an_already_committed_append : Specification
{
    Exception? _error;
    IEventSequence _sequence;
    KeyedSequenceProxy _proxy;
    OutboxEntry _entry;

    async Task Because()
    {
        _sequence = KeyedSequenceProxy.Create(out _proxy);
        _proxy.Supported = true;
        _proxy.Result = new AppendResult { Outcome = AppendOutcome.AlreadyAppended };
        var store = Substitute.For<IEventStore>();
        store.GetEventSequence(EventSequenceId.Log).Returns(_sequence);
        _entry = OutboxEntry.Create(FactKind.FamilyStarted, "grant", FenceDoubles.Subject, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(30));
        _error = await Catch.Exception(() => new ChronicleRevocationFactWriter(store).Append(_entry, CancellationToken.None));
    }
    [Fact] void should_treat_the_already_committed_append_as_success() => _error.ShouldBeNull();
    [Fact] void should_use_the_persisted_fact_and_type_identity() => _proxy.Calls.Single(call => call.Name == "AppendOnce").Arguments[2].ToString().ShouldEqual(_entry.Id);
    [Fact] void should_not_use_an_unkeyed_append() => _proxy.Calls.Exists(call => call.Name == "Append").ShouldBeFalse();
}

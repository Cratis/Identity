// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

public class when_the_kernel_does_not_support_keyed_appends : Specification
{
    IEventSequence _sequence;
    KeyedSequenceProxy _proxy;
    Exception? _error;

    async Task Because()
    {
        _sequence = KeyedSequenceProxy.Create(out _proxy);
        _proxy.Supported = false;
        var store = Substitute.For<IEventStore>();
        store.GetEventSequence(EventSequenceId.Log).Returns(_sequence);
        var entry = OutboxEntry.Create(FactKind.FamilyStarted, "grant", FenceDoubles.Subject, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(30));
        _error = await Catch.Exception(() => new ChronicleRevocationFactWriter(store).Append(entry, CancellationToken.None));
    }
    [Fact] void should_hold_the_intent() => _error.ShouldBeOfExactType<RevocationPending>();
    [Fact] void should_never_fall_back_to_an_unkeyed_append() => _proxy.Calls.Exists(call => call.Name.StartsWith("Append", StringComparison.Ordinal)).ShouldBeFalse();
}

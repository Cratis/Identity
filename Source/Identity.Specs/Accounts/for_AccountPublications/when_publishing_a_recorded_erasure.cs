// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Testing.Reactors;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Accounts.for_AccountPublications;

public class when_publishing_a_recorded_erasure : Specification
{
    EventStoreForTesting _store;
    ReactorScenario<AccountPublications> _scenario;
    AccountSubject _account;
    ErasureId _erasure;
    FactId _fact;
    object[] _published;
    string[] _identities;

    void Establish()
    {
        _store = new();
        _scenario = new(eventStore: _store);
        _scenario.Services.AddSingleton<IEventStore>(_store);
        _account = AccountSubject.New();
        _erasure = ErasureId.New();
        _fact = FactId.ForOperation(new FactId(_erasure.Value), $"{nameof(AccountErasureRecorded)}:{_account.Value}");
    }

    async Task Because()
    {
        var recorded = new AccountErasureRecorded(_erasure, DateTimeOffset.UnixEpoch, new BoundaryVersion(7), _fact);
        await _scenario.Given.ForEventSource(_account.Value).Events(recorded, recorded);
        var published = (await _store.GetEventSequence(EventSequenceId.Outbox).GetForEventSourceIdAndEventTypes(_account.Value, [_store.EventTypes.GetEventTypeFor(typeof(AccountErased))])).ToArray();
        _published = published.Select(@event => @event.Content).ToArray();
        _identities = published.Select(@event => @event.Context.Tags.Single().Value).ToArray();
    }

    [Fact] void should_publish_the_public_contract_once() => _published.Length.ShouldEqual(1);
    [Fact] void should_carry_the_source_fields() => ((AccountErased)_published[0]).ShouldEqual(new AccountErased(_erasure, DateTimeOffset.UnixEpoch, new BoundaryVersion(7)));
    [Fact] void should_use_fact_id_and_contract_type_id() => _identities[0].ShouldEqual($"event-identity:{_fact.Value:D}:AccountErased");

    void Destroy()
    {
        _scenario.Dispose();
        _store.Dispose();
    }
}

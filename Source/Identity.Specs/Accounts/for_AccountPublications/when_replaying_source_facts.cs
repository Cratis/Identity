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

public class when_replaying_source_facts : Specification
{
    EventStoreForTesting _store;
    ReactorScenario<AccountPublications> _scenario;
    AccountSubject _account;
    FactId _operation;
    string[] _identities;
    Type[] _contracts;

    void Establish()
    {
        _store = new();
        _scenario = new(eventStore: _store);
        _scenario.Services.AddSingleton<IEventStore>(_store);
        _account = AccountSubject.New();
        _operation = FactId.New();
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_account.Value).Events(
            Registration(_operation),
            Registration(_operation),
            Registration(FactId.New()),
            new UpstreamKeyLinkRecorded(Guid.NewGuid(), Guid.NewGuid(), "https://github.com", "v1.digest", Guid.NewGuid(), DateTimeOffset.UnixEpoch, FactId.ForOperation(_operation, nameof(UpstreamKeyLinkRecorded))));
        var types = new[] { typeof(AccountRegistered), typeof(UpstreamKeyLinked) }.Select(_store.EventTypes.GetEventTypeFor);
        var published = (await _store.GetEventSequence(EventSequenceId.Outbox).GetForEventSourceIdAndEventTypes(_account.Value, types)).ToArray();
        _identities = published.Select(@event => @event.Context.Tags.Single().Value).ToArray();
        _contracts = published.Select(@event => @event.Content.GetType()).ToArray();
    }

    [Fact] void should_publish_a_retry_only_once() => _identities.Length.ShouldEqual(3);
    [Fact] void should_distinguish_different_facts() => _identities.Distinct().Count().ShouldEqual(3);
    [Fact] void should_use_fact_id_and_contract_type_id() => _identities[0].ShouldEqual($"event-identity:{FactId.ForOperation(_operation, nameof(AccountRegistrationRecorded)).Value:D}:AccountRegistered");
    [Fact] void should_preserve_the_supplied_source_order() => _contracts.ShouldEqual([typeof(AccountRegistered), typeof(AccountRegistered), typeof(UpstreamKeyLinked)]);

    static AccountRegistrationRecorded Registration(FactId operation) => new(DateTimeOffset.UnixEpoch, FactId.ForOperation(operation, nameof(AccountRegistrationRecorded)));

    void Destroy()
    {
        _scenario.Dispose();
        _store.Dispose();
    }
}

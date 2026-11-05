// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Testing.Reactors;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Identity.Accounts.for_AccountPublications;

public class when_publication_fails : Specification
{
    EventStoreForTesting _types;
    ReactorScenario<AccountPublications> _scenario;
    given.PublicationLog _logger;
    AccountSubject _account;
    Exception _error;
    int _attempts;

    void Establish()
    {
        _types = new();
        _account = AccountSubject.New();
        _logger = new();
        var store = Substitute.For<IEventStore>();
        store.EventTypes.Returns(_types.EventTypes);
        store.GetEventSequence(EventSequenceId.Outbox).Returns(_ =>
        {
            _attempts++;
            throw new InvalidOperationException("Injected outbox failure");
        });
        _scenario = new(eventStore: _types);
        _scenario.Services.AddSingleton<ILogger<AccountPublications>>(_logger);
        _scenario.Services.AddSingleton(store);
    }

    async Task Because() => _error = await Catch.Exception(async () => await _scenario.Given.ForEventSource(_account.Value).Events(
        new AccountRegistrationRecorded(DateTimeOffset.UnixEpoch, FactId.New()),
        new AccountTombstoneRecorded(DateTimeOffset.UnixEpoch, FactId.New())));

    [Fact] void should_rethrow_to_hold_delivery() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_publish_a_later_fact_in_that_delivery() => _attempts.ShouldEqual(1);
    [Fact] void should_log_the_stuck_account_and_source_sequence() => _logger.Errors.Exists(message => message.Contains(_account.Value, StringComparison.Ordinal) && message.Contains("source sequence", StringComparison.Ordinal)).ShouldBeTrue();

    void Destroy()
    {
        _scenario.Dispose();
        _types.Dispose();
    }
}

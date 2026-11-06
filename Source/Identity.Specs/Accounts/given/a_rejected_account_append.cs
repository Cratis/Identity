// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.given;
using Microsoft.Extensions.Logging;

namespace Cratis.Identity.Accounts.given;

public class a_rejected_account_append : Specification
{
    protected AccountSubject _account;
    protected rejecting_account_store _store;
    internal AccountStore.GuardedAccountRead _read;
    protected AppendFailureLog<AccountStore> _log;
    EventStoreForTesting _events;

    async Task Establish()
    {
        _events = new();
        _account = AccountSubject.New();
        _log = new();
        _store = new(_events, Substitute.For<IReadModels>(), _log);
        _read = await _store.Read(_account);
    }

    protected Task<bool> TryAppend() => _store.TryAppend(_account, _read, [new AccountEmailChanged("private@example.test")]);

    void Destroy() => _events.Dispose();

    public class rejecting_account_store(IEventStore events, IReadModels reader, ILogger<AccountStore> logger)
        : AccountStore(events, reader, TimeProvider.System, logger)
    {
        internal AppendManyResult Result { get; set; } = new();

        internal override Task<AppendManyResult> AppendGuarded(IEnumerable<EventForEventSourceId> events, DecisionRead<AccountGuard> guard) => Task.FromResult(Result);
    }
}

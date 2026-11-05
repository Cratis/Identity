// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.for_AccountStore;

public class when_an_account_changes_after_a_guarded_read : Specification
{
    EventStoreForTesting _store;
    AccountSubject _account;
    DecisionRead<AccountGuard> _guard;
    AppendManyResult _result;
    int _names;

    async Task Establish()
    {
        _store = new();
        _account = AccountSubject.New();
        await _store.EventLog.Append(_account.Value, new AccountRegistrationRecorded(DateTimeOffset.UnixEpoch, FactId.New()));
        _guard = await _store.GetDecisionReads().GetDetached<AccountGuard>(new ReadModelKey(_account.Value));
        await _store.EventLog.Append(_account.Value, new AccountEmailChanged("changed@example.test"));
    }

    async Task Because()
    {
        _result = await _store.EventLog.AppendMany([new EventForEventSourceId(_account.Value, new AccountNameChanged("Not committed"))], [_guard]);
        _names = (await _store.EventLog.GetForEventSourceIdAndEventTypes(_account.Value, [_store.EventTypes.GetEventTypeFor(typeof(AccountNameChanged))])).Count;
    }

    [Fact] void should_refuse_the_stale_read() => _result.HasConcurrencyViolations.ShouldBeTrue();
    [Fact] void should_have_checked_the_guard() => _result.ConcurrencyCheckPerformed.ShouldBeTrue();
    [Fact] void should_not_append_any_part_of_the_batch() => _names.ShouldEqual(0);

    void Destroy() => _store.Dispose();
}

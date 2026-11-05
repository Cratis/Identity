// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.for_AccountStore;

public class when_an_account_changes_before_a_no_op_decision_completes : Specification
{
    EventStoreForTesting _events;
    AccountStore _store;
    AccountSubject _account;
    AccountStore.GuardedAccountRead _read;
    Exception _error;

    async Task Establish()
    {
        _events = new();
        _account = AccountSubject.New();
        await _events.EventLog.Append(_account.Value, new AccountRegistrationRecorded(DateTimeOffset.UnixEpoch, FactId.New()));
        var reader = Substitute.For<IReadModels>();
        reader.GetInstanceById<AccountLinkState>(Arg.Any<ReadModelKey>(), Arg.Any<ReadModelSessionId?>()).Returns(new AccountLinkState(_account.Value) { Registered = true });
        _store = new(_events, reader);
        _read = await _store.Read(_account);
        await _events.EventLog.Append(_account.Value, new AccountDisablementRecorded("disabled", 1, Guid.NewGuid(), 1, FactId.New()));
    }

    async Task Because() => _error = await Catch.Exception(async () => await _store.Append(_account, _read, []));

    [Fact] void should_validate_even_without_profile_changes() => _error.ShouldBeOfExactType<AccountOperationRefused>();
    [Fact] void should_refuse_the_stale_decision() => _error.Message.ShouldEqual("account_concurrency_conflict");

    void Destroy() => _events.Dispose();
}

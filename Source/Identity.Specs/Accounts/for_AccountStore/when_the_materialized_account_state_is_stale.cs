// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Testing.Events;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ClientReadModels = Cratis.Chronicle.ReadModels.ReadModels;

namespace Cratis.Identity.Accounts.for_AccountStore;

public class when_the_materialized_account_state_is_stale : Specification
{
    EventStoreForTesting _events;
    AccountSubject _account;
    AccountStore _store;
    AccountStore.GuardedAccountRead _read;

    async Task Establish()
    {
        _events = new();
        _account = AccountSubject.New();
        await _events.EventLog.Append(_account.Value, new AccountRegistrationRecorded(DateTimeOffset.UnixEpoch, FactId.New()));
        await _events.EventLog.Append(_account.Value, new AccountDisablementRecorded("disabled", 1, Guid.NewGuid(), 1, FactId.New()));
        var schema = new JsonSchemaGenerator(Substitute.For<IComplianceMetadataResolver>(), new DefaultNamingPolicy(), Substitute.For<IDerivedTypes>());
        var reader = new ClientReadModels(
            _events,
            new DefaultNamingPolicy(),
            _events.Projections,
            _events.Reducers,
            _events.EventTypes,
            schema,
            Options.Create(new ChronicleOptions()),
            new JsonSerializerOptions(),
            Substitute.For<IReadModelWatcherManager>(),
            Substitute.For<IReducerObservers>(),
            Substitute.For<IMaterializedReadModels>(),
            NullLogger<ClientReadModels>.Instance);
        _store = new(_events, reader);
    }

    async Task Because() => _read = await _store.Read(_account);

    [Fact] void should_reduce_the_committed_disablement_immediately() => _read.State.Disabled.ShouldBeTrue();
    [Fact] void should_require_the_native_passive_reducer_route() => typeof(AccountLinkState).GetCustomAttribute<PassiveAttribute>().ShouldNotBeNull();

    void Destroy() => _events.Dispose();
}

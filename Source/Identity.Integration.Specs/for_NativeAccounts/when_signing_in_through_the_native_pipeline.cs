// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Registrations;
using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Upstream;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Cratis.Identity.Integration.for_NativeAccounts;

[Collection(IdentityCollection.Name)]
public class when_signing_in_through_the_native_pipeline(IdentityHost host) : IAsyncLifetime
{
    Account _account = null!;
    IEnumerable<object> _defaultEvents = [];
    IEnumerable<object> _tenantEvents = [];
    Account? _tenantAccount;
    bool _legacyCollection;

    public async ValueTask InitializeAsync()
    {
        await using var scope = host.Services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var operation = FactId.New();
        var authentication = new GitHubConnection(GitHubConnection.FixedConnectionId, provider.GetRequiredService<TimeProvider>())
            .From(new(given.UpstreamUserIds.Next(), "native-pipeline", "Native Pipeline"), operation);
        _account = await provider.GetRequiredService<EventSourcedAccountAdapter>().SignIn(authentication, new(null, "Native Pipeline", "native-pipeline"));
        var client = provider.GetRequiredService<IChronicleClient>();
        var realm = await client.GetEventStore("Identity", EventStoreNamespaceName.Default);
        var tenant = await client.GetEventStore("Identity", "tenant-alpha");
        (await tenant.WaitForRegistration(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();
        var internalEvents = (await realm.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, _account.Id.Value)).ToArray();
        var registration = internalEvents.Single(@event => @event.Content is AccountRegistrationRecorded);
        await realm.Reactors.WaitTillReachesEventSequenceNumber<AccountPublications>(registration.Context.SequenceNumber, TimeSpan.FromSeconds(10));
        var publications = (await realm.GetEventSequence(EventSequenceId.Outbox).GetFromSequenceNumber(EventSequenceNumber.First, _account.Id.Value)).Select(@event => @event.Content);
        _defaultEvents = internalEvents.Select(@event => @event.Content).Concat(publications).ToArray();
        _tenantEvents = (await tenant.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, _account.Id.Value)).Select(@event => @event.Content).ToArray();
        _tenantAccount = await tenant.ReadModels.GetInstanceById<Account>(_account.Id.Value);
        using var names = await provider.GetRequiredService<IMongoDatabase>().ListCollectionNamesAsync();
        _legacyCollection = (await names.ToListAsync()).Contains("accounts");
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_allocate_an_immutable_account_subject() => Guid.TryParse(_account.Id.Value, out _).ShouldBeTrue();
    [Fact] void should_store_internal_registration_in_the_realm() => _defaultEvents.OfType<AccountRegistrationRecorded>().ShouldContainSingleItem();
    [Fact] void should_publish_registration_in_the_realm() => _defaultEvents.OfType<AccountRegistered>().ShouldContainSingleItem();
    [Fact] void should_use_the_caught_up_account_profile() => _account.Name.Value.ShouldEqual("Native Pipeline");
    [Fact] void should_not_write_any_account_facts_to_a_tenant() => _tenantEvents.ShouldBeEmpty();
    [Fact] void should_not_materialize_a_tenant_account() => _tenantAccount.ShouldBeNull();
    [Fact] void should_not_create_a_legacy_accounts_collection() => _legacyCollection.ShouldBeFalse();
}

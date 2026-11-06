// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Identity.Accounts;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Upstream;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Integration.for_NativeAccounts;

[Collection(IdentityCollection.Name)]
public class when_two_first_sign_ins_race(IdentityHost host) : IAsyncLifetime
{
    Account[] _accounts = [];
    int _reservations;
    int _registrations;

    public async ValueTask InitializeAsync()
    {
        await using var scope = host.Services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var adapter = provider.GetRequiredService<EventSourcedAccountAdapter>();
        var connection = new GitHubConnection(GitHubConnection.FixedConnectionId, provider.GetRequiredService<TimeProvider>());
        var user = new GitHubUser(987654324, "first-sign-in-race", "Concurrent Sign In");
        var first = connection.From(user, FactId.New());
        var second = connection.From(user, FactId.New());
        _accounts = await Task.WhenAll(adapter.SignIn(first, new(null, user.Name, user.Login)), adapter.SignIn(second, new(null, user.Name, user.Login)));
        var store = provider.GetRequiredService<IEventStore>();
        var digest = provider.GetRequiredService<UpstreamKeyDigester>().Digest(first.IdpIssuer, first.Subject);
        _reservations = (await store.ReadModels.GetInstances<UpstreamKeyLookup>()).Count(owner => owner.Keys.Any(key => key.KeyHash == digest));
        var facts = new[] { FactId.ForOperation(first.OperationId, nameof(AccountRegistrationRecorded)), FactId.ForOperation(second.OperationId, nameof(AccountRegistrationRecorded)) };
        _registrations = (await store.EventLog.GetFromSequenceNumber(Chronicle.Events.EventSequenceNumber.First))
            .Select(@event => @event.Content).OfType<AccountRegistrationRecorded>().Count(@event => facts.Contains(@event.FactId));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_return_the_same_subject_to_both_callers() => _accounts.Select(account => account.Id).Distinct().ShouldContainSingleItem();
    [Fact] void should_commit_exactly_one_registration() => _registrations.ShouldEqual(1);
    [Fact] void should_leave_exactly_one_permanent_reservation_owner() => _reservations.ShouldEqual(1);
}

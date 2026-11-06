// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Identity.Accounts;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Integration.for_NativeAccounts.given;
using Cratis.Identity.Upstream;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Integration.for_NativeAccounts;

[Collection(IdentityCollection.Name)]
public class when_two_first_sign_ins_race(IdentityHost host) : IAsyncLifetime
{
    SignInOutcome[] _outcomes = [];
    NativeSignInInterleaving.Run _run = null!;
    AccountSubject _winner = null!;
    FactId _losingOperation = null!;
    int _reservations;
    int _registrations;
    object[] _loserEvents = [];

    public async ValueTask InitializeAsync()
    {
        await using var scope = host.Services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var adapter = provider.GetRequiredService<EventSourcedAccountAdapter>();
        var connection = new GitHubConnection(GitHubConnection.FixedConnectionId, provider.GetRequiredService<TimeProvider>());
        var user = new GitHubUser(987654324, "first-sign-in-race", "Concurrent Sign In");
        var first = connection.From(user, FactId.New());
        var second = connection.From(user, FactId.New());
        using var run = provider.GetRequiredService<NativeSignInInterleaving>().Begin(2);
        _run = run;
        var attempts = new Dictionary<FactId, Task<SignInOutcome>>
        {
            [first.OperationId] = SignInOutcome.Observe(adapter.SignIn(first, new(null, user.Name, user.Login))),
            [second.OperationId] = SignInOutcome.Observe(adapter.SignIn(second, new(null, user.Name, user.Login)))
        };
        var winning = await run.Next();
        var losing = await run.Next();
        winning.Decision.Reservation.ShouldBeNull();
        losing.Decision.Reservation.ShouldBeNull();
        winning.Decision.Account.ShouldBeNull();
        losing.Decision.Account.ShouldBeNull();
        winning.Decision.Subject.ShouldNotEqual(losing.Decision.Subject);
        run.Results.ShouldBeEmpty();
        _winner = winning.Decision.Subject;
        _losingOperation = losing.Decision.Authentication.OperationId;

        // Both absence decisions are now held. Commit one before releasing the stale loser.
        winning.Release();
        var winner = await attempts[winning.Decision.Authentication.OperationId];
        winner.Failure.ShouldBeNull();
        winner.Account!.Id.ShouldEqual(_winner);
        losing.Release();
        _outcomes = [winner, await attempts[_losingOperation]];

        var store = provider.GetRequiredService<IEventStore>();
        var digest = provider.GetRequiredService<UpstreamKeyDigester>().Digest(first.IdpIssuer, first.Subject);
        _reservations = (await store.ReadModels.GetInstances<UpstreamKeyLookup>()).Count(owner => owner.Keys.Any(key => key.KeyHash == digest));
        var facts = new[] { FactId.ForOperation(first.OperationId, nameof(AccountRegistrationRecorded)), FactId.ForOperation(second.OperationId, nameof(AccountRegistrationRecorded)) };
        var events = await store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
        _registrations = events.Select(@event => @event.Content).OfType<AccountRegistrationRecorded>().Count(@event => facts.Contains(@event.FactId));
        _loserEvents = (await store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, losing.Decision.Subject.Value)).Select(@event => @event.Content).ToArray();
    }

    public ValueTask DisposeAsync()
    {
        _run?.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact] void should_reject_the_stale_absence_decision_in_the_real_kernel() => _run.Results.First(attempt => attempt.OperationId == _losingOperation).Result.ValidationResults.Any(validation => validation.Reason == ValidationResultReason.ConcurrencyViolation || (validation.Reason == ValidationResultReason.ConstraintViolation && validation.ReasonDetail == "unique-upstream-key")).ShouldBeTrue();
    [Fact] void should_retry_the_loser_only_once() => _run.Results.Count(attempt => attempt.OperationId == _losingOperation).ShouldEqual(2);
    [Fact] void should_wait_for_catch_up_before_retrying() => _run.RetryWaits.ShouldEqual(1);
    [Fact] void should_redecide_using_the_winning_reservation() => _run.Decisions.Last().Reservation!.AccountSubject.ShouldEqual(_winner);
    [Fact] void should_return_only_the_winning_subject_or_a_defined_refusal() => _outcomes.All(outcome => outcome.Account is { } account ? account.Id == _winner && outcome.Failure is null : new[] { "account_concurrency_conflict", "upstream_key_already_reserved", "temporarily_unavailable" }.Contains(outcome.Failure)).ShouldBeTrue();
    [Fact] void should_commit_exactly_one_registration() => _registrations.ShouldEqual(1);
    [Fact] void should_not_commit_any_events_for_the_losing_subject() => _loserEvents.ShouldBeEmpty();
    [Fact] void should_leave_exactly_one_permanent_reservation_owner() => _reservations.ShouldEqual(1);
}

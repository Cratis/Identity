// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Integration.for_NativeAccounts.given;
using Cratis.Identity.Upstream;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Integration.for_NativeAccounts;

[Collection(IdentityCollection.Name)]
public class when_both_sign_in_attempts_lose_optimistic_concurrency(IdentityHost host) : IAsyncLifetime
{
    NativeSignInInterleaving.Run _run = null!;
    SignInOutcome _outcome = null!;
    int _newEvents;
    object[] _events = [];

    public async ValueTask InitializeAsync()
    {
        await using var scope = host.Services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var adapter = provider.GetRequiredService<EventSourcedAccountAdapter>();
        var connection = new GitHubConnection(GitHubConnection.FixedConnectionId, provider.GetRequiredService<TimeProvider>());
        var user = new GitHubUser(987654328, "bounded-concurrency", "Before Conflicts");
        var initial = await adapter.SignIn(connection.From(user, FactId.New()), new(null, user.Name, user.Login));
        var store = provider.GetRequiredService<IEventStore>();
        var before = (await store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, initial.Id.Value)).Count;
        using var run = provider.GetRequiredService<NativeSignInInterleaving>().Begin(2);
        _run = run;
        var signIn = SignInOutcome.Observe(adapter.SignIn(connection.From(user, FactId.New()), new(null, "Stale On Both Attempts", "stale-on-both-attempts")));
        var first = await run.Next();
        first.Decision.Subject.ShouldEqual(initial.Id);
        first.Decision.Account.Name.Value.ShouldEqual(user.Name);
        run.Results.ShouldBeEmpty();
        (await store.EventLog.Append(initial.Id.Value, new AccountNameChanged("First Competing Writer"))).IsSuccess.ShouldBeTrue();
        first.Release();

        var retry = await run.Next();
        retry.Decision.Subject.ShouldEqual(initial.Id);
        retry.Decision.Authentication.OperationId.ShouldEqual(first.Decision.Authentication.OperationId);
        retry.Decision.Account.Name.Value.ShouldEqual("First Competing Writer");
        signIn.IsCompleted.ShouldBeFalse();
        (await store.EventLog.Append(initial.Id.Value, new AccountNameChanged("Second Competing Writer"))).IsSuccess.ShouldBeTrue();
        retry.Release();
        _outcome = await signIn;
        _events = (await store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, initial.Id.Value)).Select(@event => @event.Content).ToArray();
        _newEvents = _events.Length - before;
    }

    public ValueTask DisposeAsync()
    {
        _run?.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact] void should_report_real_kernel_concurrency_violations_for_both_attempts() => _run.Results.All(attempt => attempt.Result.ValidationResults.Any(validation => validation.Reason == ValidationResultReason.ConcurrencyViolation)).ShouldBeTrue();
    [Fact] void should_stop_after_two_commands() => _run.Results.Count.ShouldEqual(2);
    [Fact] void should_make_only_two_fresh_decisions() => _run.Decisions.Count.ShouldEqual(2);
    [Fact] void should_wait_for_catch_up_only_before_the_single_retry() => _run.RetryWaits.ShouldEqual(1);
    [Fact] void should_fail_closed_when_the_retry_also_conflicts() => _outcome.Failure.ShouldEqual("account_concurrency_conflict");
    [Fact] void should_not_return_an_account_for_session_construction() => _outcome.Account.ShouldBeNull();
    [Fact] void should_append_only_the_two_competing_events() => _newEvents.ShouldEqual(2);
    [Fact] void should_not_append_either_stale_name_decision() => _events.OfType<AccountNameChanged>().Any(@event => @event.PersonalName.Value == "Stale On Both Attempts").ShouldBeFalse();
    [Fact] void should_not_append_either_stale_login_decision() => _events.OfType<UpstreamKeyDisplayIdentifierObserved>().Any(@event => @event.Login.Value == "stale-on-both-attempts").ShouldBeFalse();
}

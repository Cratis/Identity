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
public class when_an_upstream_key_is_unlinked_after_the_sign_in_recheck(IdentityHost host) : IAsyncLifetime
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
        var timeProvider = provider.GetRequiredService<TimeProvider>();
        var connection = new GitHubConnection(GitHubConnection.FixedConnectionId, timeProvider);
        var user = new GitHubUser(987654329, "unlinked-after-recheck", "Before Unlink");
        var initial = await adapter.SignIn(connection.From(user, FactId.New()), new(null, user.Name, user.Login));
        var store = provider.GetRequiredService<IEventStore>();
        var before = (await store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, initial.Id.Value)).Count;
        using var run = provider.GetRequiredService<NativeSignInInterleaving>().Begin(1);
        _run = run;
        var signIn = SignInOutcome.Observe(adapter.SignIn(connection.From(user, FactId.New()), new(null, "Stale Sign In Name", "stale-sign-in-login")));
        var paused = await run.Next();
        paused.Decision.Subject.ShouldEqual(initial.Id);
        paused.Decision.Reservation.Active.ShouldBeTrue();
        paused.Decision.Account.Status.ShouldEqual(AccountStatus.Active);
        paused.Decision.Account.LinkedUpstreamKeys.Single().LinkId.ShouldEqual(paused.Decision.Reservation.LinkId);
        signIn.IsCompleted.ShouldBeFalse();
        run.Results.ShouldBeEmpty();

        // The guarded read and final rechecks have completed; Arc has not received the events yet.
        var append = await store.EventLog.Append(initial.Id.Value, new UpstreamKeyUnlinkRecorded(paused.Decision.Reservation.LinkId, paused.Decision.Digest, "spec-unlink", timeProvider.GetUtcNow(), FactId.New()));
        append.IsSuccess.ShouldBeTrue();
        paused.Release();
        _outcome = await signIn;
        _events = (await store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, initial.Id.Value)).Select(@event => @event.Content).ToArray();
        _newEvents = _events.Length - before;
    }

    public ValueTask DisposeAsync()
    {
        _run?.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact] void should_reject_the_pre_unlink_decision_with_real_optimistic_concurrency() => _run.Results.First().Result.ValidationResults.Any(validation => validation.Reason == ValidationResultReason.ConcurrencyViolation).ShouldBeTrue();
    [Fact] void should_execute_only_one_fresh_retry() => _run.Results.Count.ShouldEqual(2);
    [Fact] void should_make_only_two_fresh_decisions() => _run.Decisions.Count.ShouldEqual(2);
    [Fact] void should_wait_for_projection_catch_up_before_retry() => _run.RetryWaits.ShouldEqual(1);
    [Fact] void should_retry_on_the_original_reserved_subject() => _run.Decisions.Last().Subject.ShouldEqual(_run.Decisions.First().Subject);
    [Fact] void should_preserve_the_original_reservation() => _run.Decisions.Last().Reservation.LinkId.ShouldEqual(_run.Decisions.First().Reservation.LinkId);
    [Fact] void should_recheck_the_inactive_reservation() => _run.Decisions.Last().Reservation.Active.ShouldBeFalse();
    [Fact] void should_recheck_the_removed_link() => _run.Decisions.Last().Account.LinkedUpstreamKeys.ShouldBeEmpty();
    [Fact] void should_keep_the_account_active() => _run.Decisions.Last().Account.Status.ShouldEqual(AccountStatus.Active);
    [Fact] void should_fail_closed_with_the_defined_inactive_key_refusal() => _outcome.Failure.ShouldEqual("upstream_key_reserved_but_inactive");
    [Fact] void should_not_return_an_account_for_session_construction() => _outcome.Account.ShouldBeNull();
    [Fact] void should_append_only_the_competing_unlink_event() => _newEvents.ShouldEqual(1);
    [Fact] void should_commit_the_unlink() => _events.OfType<UpstreamKeyUnlinkRecorded>().ShouldContainSingleItem();
    [Fact] void should_not_register_a_replacement_account() => _events.OfType<AccountRegistrationRecorded>().ShouldContainSingleItem();
    [Fact] void should_not_append_a_replacement_link() => _events.OfType<UpstreamKeyLinkRecorded>().ShouldContainSingleItem();
    [Fact] void should_not_append_the_stale_name_change() => _events.OfType<AccountNameChanged>().Any(@event => @event.PersonalName.Value == "Stale Sign In Name").ShouldBeFalse();
    [Fact] void should_not_append_the_stale_login_change() => _events.OfType<UpstreamKeyDisplayIdentifierObserved>().Any(@event => @event.Login.Value == "stale-sign-in-login").ShouldBeFalse();
}

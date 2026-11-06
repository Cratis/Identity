// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.SigningIn.when_signing_in;

public class and_an_unlink_commits_after_the_recheck : given.a_sign_in_decision
{
    EventStoreForTesting _store;
    UnitOfWork _unit;
    EventSequenceNumber _tailAfterUnlink;
    EventSequenceNumber _tailAfterDecision;

    async Task Establish()
    {
        _store = new();
        await _store.EventLog.AppendMany(_subject.Value,
        [
            new AccountRegistrationRecorded(_clock.Now, FactId.New()),
            new UpstreamKeyLinkRecorded(_link, _verified.Authentication!.ConnectionId, "https://github.com", _digest, Guid.NewGuid(), _clock.Now, FactId.New())
        ]);
    }

    async Task Because()
    {
        _unit = (UnitOfWork)_store.UnitOfWorkManager.Begin(Cratis.Execution.CorrelationId.New());
        using var frame = _unit.EnforceReadProtection();
        var owner = _unit.ClaimDecisionReadCommitOwnership();
        _decisions.Get<AccountGuard>(Arg.Any<ReadModelKey>()).Returns(call => _store.GetDecisionReads().GetFor<AccountGuard>(_unit, call.Arg<ReadModelKey>()));
        var provided = await _command.Provide(_verified, _gate, _lookups, _decisions, new AccountReader(_readModels), _catchUp, _clock);
        Task competitor;
        using (ExecutionContext.SuppressFlow())
        {
            competitor = Task.Run(async () =>
            {
                var append = await _store.EventLog.Append(_subject.Value, new UpstreamKeyUnlinkRecorded(_link, _digest, "user_requested", _clock.Now, FactId.New()));
                append.IsSuccess.ShouldBeTrue();
                _tailAfterUnlink = await _store.EventLog.GetTailSequenceNumber();
            });
        }

        await competitor;
        foreach (var item in _command.Handle(provided.AsT0).AsT0.Item1)
        {
            _unit.AddEvent(EventSequenceId.Log, item.EventSourceId, item.Event, Causation.Unknown());
        }

        await _unit.CommitAsOwner(owner);

        // The inspection runs outside the command's protected read lifetime.
        frame.Dispose();
        _tailAfterDecision = await _store.EventLog.GetTailSequenceNumber();
    }

    void Destroy() => _store.Dispose();

    [Fact] void should_refuse_the_stale_decision() => _unit.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_source_concurrency_conflict() => _unit.GetConcurrencyViolations().ShouldNotBeEmpty();
    [Fact] void should_append_no_stale_profile_events() => _tailAfterDecision.ShouldEqual(_tailAfterUnlink);
}

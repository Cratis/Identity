// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding.when_approving;

public class and_the_approval_goes_through_the_command : ConsentFlow.given.ConsentPathDriver
{
    ConsentRequestState _decided;
    int _factsWhenDecided;
    int _preparedWhenDecided;
    bool _usableWhenDecided;
    string? _earlyCode;
    ConsentRequestState _reconciled;
    bool _usableWhenReconciled;
    string? _firstCode;
    string? _secondCode;

    async Task Because()
    {
        await Approve();
        _decided = await State();
        _factsWhenDecided = AppendedFacts;
        _preparedWhenDecided = PreparedAuthorizations;
        _usableWhenDecided = await AuthorizationUsable();
        _earlyCode = await ClaimCode();
        await Reconcile();
        _reconciled = await State();
        _usableWhenReconciled = await AuthorizationUsable();
        _firstCode = await ClaimCode();
        _secondCode = await ClaimCode();
    }

    [Fact] void should_only_record_the_decision() => _decided.ShouldEqual(ConsentRequestState.Decided);
    [Fact] void should_append_nothing_at_decision_time() => _factsWhenDecided.ShouldEqual(0);
    [Fact] void should_prepare_nothing_at_decision_time() => _preparedWhenDecided.ShouldEqual(0);
    [Fact] void should_not_be_usable_at_decision_time() => _usableWhenDecided.ShouldBeFalse();
    [Fact] void should_not_issue_a_code_at_decision_time() => _earlyCode.ShouldBeNull();
    [Fact] void should_activate_when_reconciled() => _reconciled.ShouldEqual(ConsentRequestState.Active);
    [Fact] void should_be_usable_once_active() => _usableWhenReconciled.ShouldBeTrue();
    [Fact] void should_append_one_evidence_pair() => AppendedFacts.ShouldEqual(1);
    [Fact] void should_issue_the_code_once() => _firstCode.ShouldNotBeNull();
    [Fact] void should_not_issue_the_code_twice() => _secondCode.ShouldBeNull();
}

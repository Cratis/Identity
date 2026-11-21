// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.ReadModels;
using Cratis.Monads;

namespace Cratis.Identity.Accounts.Linking.Unlinking.when_unlinking_a_key;

public class and_reading_the_decision_state : given.a_link_decision
{
    Result<UnlinkUpstreamKey.Decision, ValidationResult> _result;

    async Task Because() => _result = await new UnlinkUpstreamKey(Guid.NewGuid()).Provide(_ceremony, _decisions, new AccountReader(_readModels), _clock);

    [Fact] void should_enroll_before_reading_state() => _order.ShouldEqual(["guard", "state"]);
    [Fact] void should_guard_the_session_account() => _guardKeys.ShouldEqual([new ReadModelKey(_account.Value)]);
    [Fact] void should_read_the_guarded_account() => _stateKeys.ShouldEqual(_guardKeys);
    [Fact] void should_supply_the_snapshot() => _result.AsT0.State.ShouldEqual(_state);
}

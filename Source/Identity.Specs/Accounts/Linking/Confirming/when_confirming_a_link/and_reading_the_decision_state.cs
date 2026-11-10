// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Identity.Accounts.Linking.Confirming.when_confirming_a_link;

public class and_reading_the_decision_state : given.a_confirmation_decision
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_enroll_before_reading_account_state(bool clearing)
    {
        var result = await Provide(clearing);
        result.TryGetError(out _).ShouldBeFalse();
        _order.ShouldEqual(["guard", "state", "reservation"]);
        _guardKeys.ShouldEqual([new ReadModelKey(_account.Value)]);
        _stateKeys.ShouldEqual(_guardKeys);
        result.AsT0.State.ShouldEqual(_state);
    }
}

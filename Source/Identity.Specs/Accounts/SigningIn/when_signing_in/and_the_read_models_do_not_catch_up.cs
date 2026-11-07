// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Identity.Protocol;

namespace Cratis.Identity.Accounts.SigningIn.when_signing_in;

public class and_the_read_models_do_not_catch_up : given.a_sign_in_decision
{
    Exception _error;

    void Establish() => _progress.Position(Arg.Any<Type>()).Returns(Task.FromResult(new ObserverPosition(ObserverRunningState.Suspended, false, false, new(0))));

    async Task Because()
    {
        try
        {
            await Decide();
        }
        catch (ReadModelsNotCaughtUp error)
        {
            _error = error;
        }
    }

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<ReadModelsNotCaughtUp>();
    [Fact] void should_produce_no_events() => _events.ShouldBeEmpty();
    [Fact] void should_not_read_an_account_before_catching_up() => _order.ShouldNotContain("account");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Identity.Accounts.Linking.for_AccountLinking;

public class when_the_first_append_conflicts : given.an_account_linking_adapter
{
    void Establish() => _results.Enqueue(new CommandResult { ValidationResults = [ValidationResult.Error("Conflict") with { Reason = ValidationResultReason.ConcurrencyViolation }] });

    Task Because() => Propose();

    [Fact] void should_use_the_fresh_result() => _proposed.ShouldEqual(_proposal);
    [Fact] void should_wait_between_attempts() => _order.ShouldEqual(["execute", "wait", "execute"]);
    [Fact] void should_supply_the_original_session_each_time() => _attempts.TrueForAll(attempt => attempt.Session == _ceremony.Session).ShouldBeTrue();
    [Fact] void should_supply_the_original_verified_proof_each_time() => _attempts.TrueForAll(attempt => attempt.Authentication == _ceremony.Authentication).ShouldBeTrue();
    [Fact] void should_preserve_explicit_intent() => _attempts.TrueForAll(attempt => attempt.Intent.Explicit).ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.SigningIn.when_signing_in;

public class and_the_operation_id_does_not_match : given.a_sign_in_decision
{
    void Establish() => _command = new(FactId.New());
    Task Because() => Decide();
    [Fact] void should_refuse() => _refusal.ShouldEqual("operation_id_mismatch");
    [Fact] void should_produce_no_events() => _events.ShouldBeEmpty();
}

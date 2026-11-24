// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts.SigningIn.given;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.SigningIn.when_signing_in;

public class and_an_existing_account_signs_in_again : a_sign_in_decision
{
    async Task Because() => await Decide();

    [Fact] void should_record_exactly_one_sign_in() => _events.OfType<AccountSignInRecorded>().ShouldContainSingleItem();
    [Fact] void should_record_the_sign_in_last() => _events.Last().ShouldBeOfExactType<AccountSignInRecorded>();
    [Fact] void should_not_register_the_account_again() => _events.OfType<AccountRegistrationRecorded>().ShouldBeEmpty();
    [Fact] void should_record_the_connection_and_issuer_that_proved_it() => _events.OfType<AccountSignInRecorded>().Single().IdpIssuer.ShouldEqual(_verified.Authentication!.IdpIssuer);
    [Fact] void should_record_the_server_time() => _events.OfType<AccountSignInRecorded>().Single().SignedInAt.ShouldEqual(_clock.GetUtcNow());
    [Fact] void should_derive_a_stable_fact_id_from_the_operation() => _events.OfType<AccountSignInRecorded>().Single().FactId.ShouldEqual(FactId.ForOperation(_verified.Authentication!.OperationId, nameof(AccountSignInRecorded)));
}

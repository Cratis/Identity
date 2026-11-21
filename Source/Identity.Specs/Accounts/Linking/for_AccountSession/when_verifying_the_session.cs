// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Validation;
using Cratis.Identity.Accounts.given;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Linking.for_AccountSession;

public class when_verifying_the_session : Specification
{
    AccountClock _clock;
    AccountSubject _account;
    ValidationResult _refusal;
    AccountSession _verified;

    void Establish()
    {
        _clock = new();
        _account = AccountSubject.New();
    }

    void Because()
    {
        AccountSession.Verify(new ClaimsPrincipal(new ClaimsIdentity()), _clock).TryGetError(out _refusal);
        _verified = AccountSession.Verify(EventSourcedAccountPrincipal.For(_account, "display", _clock), _clock).AsT0;
    }

    [Fact] void should_wrap_the_refusal_message_of_an_unauthenticated_principal() => _refusal.Message.ShouldEqual("existing_account_proof_required");
    [Fact] void should_return_the_session_of_an_authenticated_principal() => _verified.Account.ShouldEqual(_account);
}

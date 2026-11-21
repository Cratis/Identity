// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts.SigningIn.when_signing_in;

public class and_the_profile_is_not_observed : Specification
{
    IEnumerable<object> _events;

    void Because()
    {
        var subject = AccountSubject.New();
        var authentication = new VerifiedAuthentication(GitHubConnection.FixedConnectionId, "https://github.com", "42", DateTimeOffset.UtcNow, FactId.New());
        var decision = new SignInWithUpstreamKey.Decision(subject, authentication, new("v1.test-digest"), new(null, "Name", "login"), null, null, DateTimeOffset.UtcNow);
        _events = new SignInWithUpstreamKey(authentication.OperationId).Handle(decision).AsT0.Item1.Select(item => item.Event);
    }

    [Fact] void should_not_record_an_unobserved_email() => _events.OfType<AccountEmailChanged>().ShouldBeEmpty();
    [Fact] void should_register_the_account() => _events.OfType<AccountRegistrationRecorded>().ShouldContainSingleItem();
}

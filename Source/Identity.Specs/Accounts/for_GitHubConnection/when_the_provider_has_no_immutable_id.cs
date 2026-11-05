// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts.for_GitHubConnection;

public class when_the_provider_has_no_immutable_id : given.a_link_proposal
{
    string _login;
    string _email;

    async Task Because()
    {
        var connection = new GitHubConnection(new(Guid.NewGuid()), _clock);
        _login = await Refusal(() => Task.FromResult(connection.From(new(0, "mutable-login", "Person"), FactId.New())));
        _email = await Refusal(() => Task.FromResult(connection.From(new(-1, "person@example.test", "Person"), FactId.New())));
        await Refresh();
    }

    [Fact] void should_never_substitute_login_for_the_missing_id() => _login.ShouldEqual("invalid_upstream_subject");
    [Fact] void should_never_substitute_email_for_the_missing_id() => _email.ShouldEqual("invalid_upstream_subject");
    [Fact] void should_not_create_an_account() => _states.Instances.Count.ShouldEqual(1);
    [Fact] void should_not_create_a_link() => _states.InstanceForEventSourceId(_account.Value).Links.Count.ShouldEqual(1);
    [Fact] void should_not_create_a_proposal() => _states.InstanceForEventSourceId(_account.Value).Proposals.ShouldBeEmpty();
}

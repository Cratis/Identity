// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts.for_Account;

public class when_looking_up_linked_keys : Specification
{
    const string OtherIssuer = "https://accounts.example.test";

    Account _account;
    Account _unlinked;

    void Establish()
    {
        var earlier = new LinkedUpstreamKey(new(Guid.NewGuid()), new(Guid.NewGuid()), OtherIssuer, "other-sub", "other-login", DateTimeOffset.UnixEpoch);
        var later = new LinkedUpstreamKey(new(Guid.NewGuid()), GitHubConnection.FixedConnectionId, GitHubConnection.Issuer, "12345", "git-login", DateTimeOffset.UnixEpoch.AddDays(1));
        _account = new(AccountSubject.New(), "Person", null, AccountStatus.Active, [later, earlier]);
        _unlinked = new(AccountSubject.New(), "Person", null, AccountStatus.Active, []);
    }

    [Fact] void should_find_the_link_of_the_exact_issuer() => _account.LinkFor(GitHubConnection.Issuer).IdpSub.Value.ShouldEqual("12345");
    [Fact] void should_not_match_a_different_issuer_case() => _account.LinkFor("https://GitHub.com").ShouldBeNull();
    [Fact] void should_find_nothing_for_an_issuer_without_a_link() => _unlinked.LinkFor(GitHubConnection.Issuer).ShouldBeNull();
    [Fact] void should_use_the_earliest_link_as_the_display_login() => _account.DisplayLogin().Value.ShouldEqual("other-login");
    [Fact] void should_have_no_display_login_without_links() => _unlinked.DisplayLogin().ShouldBeNull();
}

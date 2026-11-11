// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;

namespace Cratis.Identity.for_UserInfoClaims;

public class when_projecting_profile : for_OidcIdentity.given.an_identity_grant
{
    Dictionary<string, object> _withProfile;
    Dictionary<string, object> _withoutProfile;

    void Because()
    {
        _withProfile = UserInfoClaims.For(_account, true);
        _withoutProfile = UserInfoClaims.For(_account, false);
    }

    [Fact] void should_expose_only_the_three_permitted_claims() => _withProfile.Keys.ShouldContainOnly("sub", "name", "preferred_username");
    [Fact] void should_use_the_current_canonical_subject() => _withProfile["sub"].ShouldEqual(_account.Id.Value);
    [Fact] void should_read_the_current_name() => _withProfile["name"].ShouldEqual(_account.Name.Value);
    [Fact] void should_read_the_current_username() => _withProfile["preferred_username"].ShouldEqual(_account.GitHubLogin.Value);
    [Fact] void should_return_only_sub_without_profile() => _withoutProfile.Keys.ShouldContainOnly("sub");
}

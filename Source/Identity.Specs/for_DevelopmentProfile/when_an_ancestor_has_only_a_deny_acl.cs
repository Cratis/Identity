// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_an_ancestor_has_only_a_deny_acl : given.a_macos_acl_profile
{
    void Establish() => AddAcl(_root, "everyone deny chown");

    void Because() => Apply();

    [Fact(SkipUnless = nameof(IsMacOS), SkipType = typeof(given.a_macos_acl_profile), Skip = "Requires macOS extended ACLs.")] void should_allow_provisioning() => _exception.ShouldBeNull();
}

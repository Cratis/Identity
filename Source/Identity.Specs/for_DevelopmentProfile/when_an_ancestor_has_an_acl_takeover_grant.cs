// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_an_ancestor_has_an_acl_takeover_grant : given.a_macos_acl_profile
{
    void Establish() => AddAcl(_root, "everyone allow delete_child,writesecurity,chown");

    void Because() => Apply();

    [Fact(SkipUnless = nameof(IsMacOS), SkipType = typeof(given.a_macos_acl_profile), Skip = "Requires macOS extended ACLs.")] void should_refuse_the_takeover_grant() => _exception.ShouldBeOfExactType<DevelopmentProfileRefused>();
    [Fact(SkipUnless = nameof(IsMacOS), SkipType = typeof(given.a_macos_acl_profile), Skip = "Requires macOS extended ACLs.")] void should_identify_the_extended_acl() => _exception.Message.ShouldContain("extended ACL");
}

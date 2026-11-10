// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_an_ancestor_has_an_inherit_only_acl_grant : given.a_macos_acl_profile
{
    void Establish() => AddAcl(_root, "everyone allow read,write,delete_child,file_inherit,directory_inherit,only_inherit");

    void Because() => Apply();

    [Fact(SkipUnless = nameof(IsMacOS), SkipType = typeof(given.a_macos_acl_profile), Skip = "Requires macOS extended ACLs.")] void should_refuse_the_inherited_grant() => _exception.ShouldBeOfExactType<DevelopmentProfileRefused>();
    [Fact(SkipUnless = nameof(IsMacOS), SkipType = typeof(given.a_macos_acl_profile), Skip = "Requires macOS extended ACLs.")] void should_refuse_before_creating_the_store() => Directory.Exists(_storeDirectory).ShouldBeFalse();
}

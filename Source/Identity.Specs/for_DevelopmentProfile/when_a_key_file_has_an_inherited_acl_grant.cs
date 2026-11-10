// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_a_key_file_has_an_inherited_acl_grant : given.a_macos_acl_profile
{
    void Establish()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        Apply();
        _exception.ShouldBeNull();
        var key = Directory.GetFiles(_storeDirectory, "*.key")[0];
        var staging = Path.Combine(_contentRoot, "acl-staging");
        Directory.CreateDirectory(staging);
        AddAcl(staging, "everyone allow read,file_inherit,only_inherit");
        var inherited = Path.Combine(staging, "inherited.key");
        File.WriteAllBytes(inherited, File.ReadAllBytes(key));
        File.Move(inherited, key, overwrite: true);
        if (OperatingSystem.IsMacOS())
        {
            File.SetUnixFileMode(key, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    void Because() => Apply();

    [Fact(SkipUnless = nameof(IsMacOS), SkipType = typeof(given.a_macos_acl_profile), Skip = "Requires macOS extended ACLs.")] void should_refuse_despite_owner_only_posix_mode() => _exception.ShouldBeOfExactType<DevelopmentProfileRefused>();
    [Fact(SkipUnless = nameof(IsMacOS), SkipType = typeof(given.a_macos_acl_profile), Skip = "Requires macOS extended ACLs.")] void should_identify_the_extended_acl() => _exception.Message.ShouldContain("extended ACL");
}

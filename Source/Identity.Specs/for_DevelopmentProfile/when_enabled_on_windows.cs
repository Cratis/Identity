// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_enabled_on_windows : given.a_development_profile
{
    bool _directoryOwnerOnly = true;
    bool _directoryProtected = true;
    bool _filesOwnerOnly = true;

    void Because()
    {
        Apply();
        if (OperatingSystem.IsWindows())
        {
            Inspect();
        }
    }

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_disable_inheritance_on_the_directory() => _directoryProtected.ShouldBeTrue();
    [Fact] void should_grant_only_the_current_user_on_the_directory() => _directoryOwnerOnly.ShouldBeTrue();
    [Fact] void should_grant_only_the_current_user_on_every_file() => _filesOwnerOnly.ShouldBeTrue();

    [SupportedOSPlatform("windows")]
    static bool OwnerOnly(FileSystemSecurity security, SecurityIdentifier user) =>
        security.AreAccessRulesProtected &&
        security.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>().All(_ => _.IdentityReference.Equals(user));

    [SupportedOSPlatform("windows")]
    void Inspect()
    {
        var user = WindowsIdentity.GetCurrent().User;
        var directory = new DirectoryInfo(_storeDirectory).GetAccessControl();
        _directoryProtected = directory.AreAccessRulesProtected;
        _directoryOwnerOnly = OwnerOnly(directory, user);
        _filesOwnerOnly = Directory.EnumerateFiles(_storeDirectory).All(_ => OwnerOnly(new FileInfo(_).GetAccessControl(), user));
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Development;
using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_an_attacker_creates_a_missing_ancestor : given.a_development_profile
{
    string _intermediate;
    bool _createdAfterPrecheck;

    void Establish()
    {
        _intermediate = Path.Combine(_root, "cratis");
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(_root, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupWrite | UnixFileMode.OtherWrite | UnixFileMode.StickyBit);
        }
    }

    void Because() => _exception = Catch.Exception(() =>
    {
        if (!OperatingSystem.IsWindows())
        {
            var store = new DevelopmentKeyStore(
                _root,
                _contentRoot,
                TimeProvider.System,
                beforeDirectoryCreation: directory =>
                {
                    _intermediate = Path.GetDirectoryName(Path.GetDirectoryName(directory))!;
                    Directory.Exists(_intermediate).ShouldBeFalse();
                    if (!OperatingSystem.IsWindows())
                    {
                        Directory.CreateDirectory(_intermediate, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                    }

                    _createdAfterPrecheck = true;
                },

                // Model the foreign UID without requiring privileged chown in the spec runner.
                ancestorOwner: path => path == _intermediate ? int.MaxValue : 0);
            store.Provision(StoreName);
        }
    });

    [Fact] void should_create_the_hostile_ancestor_after_the_precheck() => (OperatingSystem.IsWindows() || _createdAfterPrecheck).ShouldBeTrue();
    [Fact] void should_refuse_the_foreign_owned_ancestor() => (OperatingSystem.IsWindows() || (_exception is DevelopmentProfileRefused && _exception.Message.Contains("owned by another user"))).ShouldBeTrue();
    [Fact] void should_not_touch_store_contents() => (OperatingSystem.IsWindows() || !Directory.EnumerateFileSystemEntries(_storeDirectory).Any()).ShouldBeTrue();
}

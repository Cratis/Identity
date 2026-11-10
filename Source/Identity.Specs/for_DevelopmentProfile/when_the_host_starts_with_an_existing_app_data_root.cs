// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_the_host_starts_with_an_existing_app_data_root : given.a_development_profile
{
    void Because() => Apply();

    [Fact]
    void should_create_the_profile_folders_relative_to_the_pinned_root_and_provision_the_keys()
    {
        _exception.ShouldBeNull();
        Directory.Exists(Path.Combine(_root, "cratis", "identity-dev")).ShouldBeTrue();
        Directory.Exists(_storeDirectory).ShouldBeTrue();
        _custody.ReadKeyFiles().Count.ShouldEqual(4);
    }

    [Fact]
    void should_create_the_profile_folders_owner_only_on_posix()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        File.GetUnixFileMode(Path.Combine(_root, "cratis")).ShouldEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        File.GetUnixFileMode(Path.Combine(_root, "cratis", "identity-dev")).ShouldEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_a_key_file_is_readable_by_others : given.a_development_profile
{
    void Establish()
    {
        Apply();
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(Directory.EnumerateFiles(_storeDirectory, "signing.*.key").Single(), UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
        }
    }

    void Because() => Apply();

    [Fact] void should_refuse_it() => (OperatingSystem.IsWindows() || _exception is DevelopmentProfileRefused).ShouldBeTrue();
}

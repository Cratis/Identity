// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_the_directory_is_readable_by_others : given.a_development_profile
{
    void Establish()
    {
        Apply();
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(_storeDirectory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute);
        }
    }

    void Because() => Apply();

    [Fact] void should_refuse_it() => (OperatingSystem.IsWindows() || _exception is DevelopmentProfileRefused).ShouldBeTrue();
}

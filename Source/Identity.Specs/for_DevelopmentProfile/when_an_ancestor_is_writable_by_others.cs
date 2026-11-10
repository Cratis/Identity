// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_an_ancestor_is_writable_by_others : given.a_development_profile
{
    void Establish()
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(_root, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupWrite | UnixFileMode.OtherWrite);
        }
    }

    void Because() => Apply();

    [Fact] void should_refuse_it() => (OperatingSystem.IsWindows() || _exception is DevelopmentProfileRefused).ShouldBeTrue();
    [Fact] void should_name_the_ancestor() => (OperatingSystem.IsWindows() || _exception.Message.Contains("writable by other users")).ShouldBeTrue();
}

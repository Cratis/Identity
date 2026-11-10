// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Development;
using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_a_partial_write_remains : given.a_development_profile
{
    string _stray;

    void Establish()
    {
        Apply();
        _stray = Path.Combine(_storeDirectory, $"{DevelopmentKeyStore.TemporaryPrefix}0123456789abcdef");
        File.WriteAllText(_stray, "partial");
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(_stray, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    void Because() => Apply();

    [Fact] void should_refuse_it() => _exception.ShouldBeOfExactType<DevelopmentProfileRefused>();
    [Fact] void should_name_the_partial_write() => _exception.Message.ShouldContain("partial write");
    [Fact] void should_leave_it_for_the_developer() => File.Exists(_stray).ShouldBeTrue();
}

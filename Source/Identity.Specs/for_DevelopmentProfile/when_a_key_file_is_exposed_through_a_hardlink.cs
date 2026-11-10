// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_a_key_file_is_exposed_through_a_hardlink : given.a_development_profile
{
    public static bool IsPosix => !OperatingSystem.IsWindows();

    void Establish()
    {
        if (!OperatingSystem.IsWindows())
        {
            Apply();
            _exception.ShouldBeNull();
            var key = Directory.GetFiles(_storeDirectory, "*.key")[0];
            var alias = Path.Combine(_contentRoot, "key-alias");
            var start = new ProcessStartInfo("/bin/ln") { UseShellExecute = false };
            start.ArgumentList.Add(key);
            start.ArgumentList.Add(alias);
            using var process = Process.Start(start)!;
            process.WaitForExit();
            process.ExitCode.ShouldEqual(0);
            File.SetUnixFileMode(alias, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.OtherRead);
        }
    }

    void Because() => Apply();

    [Fact(SkipUnless = nameof(IsPosix), Skip = "Requires POSIX hardlinks and file modes.")] void should_refuse_the_exposed_inode() => _exception.ShouldBeOfExactType<DevelopmentProfileRefused>();
}

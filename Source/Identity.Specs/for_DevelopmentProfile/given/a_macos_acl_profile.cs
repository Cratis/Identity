// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Identity.for_DevelopmentProfile.given;

public class a_macos_acl_profile : a_development_profile
{
    public static bool IsMacOS => OperatingSystem.IsMacOS();

    protected static void AddAcl(string path, string entry)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        var start = new ProcessStartInfo("/bin/chmod") { UseShellExecute = false, RedirectStandardError = true };
        start.ArgumentList.Add("+a");
        start.ArgumentList.Add(entry);
        start.ArgumentList.Add(path);
        using var process = Process.Start(start)!;
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, error);
    }
}

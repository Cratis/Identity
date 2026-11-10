// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Development;
using Cratis.Identity.Hosting;
using Microsoft.Win32.SafeHandles;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_a_macos_acl_cannot_be_read : given.a_macos_acl_profile
{
    void Because() => _exception = Catch.Exception(() =>
    {
        if (OperatingSystem.IsMacOS())
        {
            // EBADF comes from acl_get_fd_np, not from the directory-open branch.
            using var handle = new SafeFileHandle(new IntPtr(-1), ownsHandle: false);
            MacExtendedAccess.RequireNoGrants(handle);
        }
    });

    [Fact(SkipUnless = nameof(IsMacOS), SkipType = typeof(given.a_macos_acl_profile), Skip = "Requires macOS extended ACLs.")] void should_fail_closed() => _exception.ShouldBeOfExactType<DevelopmentProfileRefused>();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Development;
using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_an_ancestor_is_owned_by_another_user : given.a_development_profile
{
    public static bool IsPosix => OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();

    void Because() => _exception = Catch.Exception(() =>
    {
        if (IsPosix)
        {
            var intermediate = Path.Combine(_root, "cratis");
            Directory.Exists(intermediate).ShouldBeTrue();

            // Model a stationary foreign UID without requiring privileged chown in the runner.
            var store = new DevelopmentKeyStore(
                _root,
                _contentRoot,
                TimeProvider.System,
                ancestorOwner: path => Path.GetFileName(path) == "cratis" ? int.MaxValue : 0);
            store.Provision(StoreName);
        }
    });

    [Fact(SkipUnless = nameof(IsPosix), Skip = "Requires POSIX ownership checks.")]
    void should_refuse_the_foreign_owner_before_creating_the_leaf()
    {
        _exception.ShouldBeOfExactType<DevelopmentProfileRefused>();
        _exception.Message.ShouldContain("owned by another user");
        Directory.Exists(_storeDirectory).ShouldBeFalse();
    }
}

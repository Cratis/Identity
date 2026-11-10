// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Development;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_the_host_prepares_the_app_data_parent : given.a_development_profile
{
    bool _leafExistedAfterBootstrap;

    void Establish() => Directory.Delete(_root, recursive: true);

    void Because()
    {
        DevelopmentProfile.EnsureParent(_root);
        _leafExistedAfterBootstrap = Directory.Exists(_storeDirectory);
        Apply();
    }

    [Fact] void should_bootstrap_app_data_outside_the_store_and_leave_only_the_leaf_for_custody_to_create()
    {
        _leafExistedAfterBootstrap.ShouldBeFalse();
        _exception.ShouldBeNull();
        Directory.Exists(_storeDirectory).ShouldBeTrue();
        _custody.ReadKeyFiles().Count.ShouldEqual(4);
    }
}

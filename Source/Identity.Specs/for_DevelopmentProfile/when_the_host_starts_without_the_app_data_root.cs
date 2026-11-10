// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Development;
using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_the_host_starts_without_the_app_data_root : given.a_development_profile
{
    void Establish() => Directory.Delete(_root, recursive: true);

    void Because() => Apply(prepareParent: () => DevelopmentProfile.EnsureParent(_root));

    [Fact]
    void should_refuse_startup_naming_the_missing_folder()
    {
        _exception.ShouldBeOfExactType<DevelopmentProfileRefused>();
        _exception.Message.ShouldContain(_root);
    }

    [Fact] void should_not_create_the_data_root_or_any_ancestor() => Directory.Exists(_root).ShouldBeFalse();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_enabled_in_staging : given.a_development_profile
{
    void Establish() => _environment.EnvironmentName = "Staging";

    void Because() => Apply();

    [Fact] void should_fail_closed() => _exception.ShouldBeOfExactType<DevelopmentProfileOnlyInDevelopment>();
    [Fact] void should_create_no_keys() => Directory.Exists(_storeDirectory).ShouldBeFalse();
}

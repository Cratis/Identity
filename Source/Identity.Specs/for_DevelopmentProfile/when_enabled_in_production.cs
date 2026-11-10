// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_enabled_in_production : given.a_development_profile
{
    void Establish() => _environment.EnvironmentName = "Production";

    void Because() => Apply();

    [Fact] void should_fail_closed() => _exception.ShouldBeOfExactType<DevelopmentProfileOnlyInDevelopment>();
    [Fact] void should_name_the_setting() => _exception.Message.ShouldContain("Identity:Development:Enabled");
    [Fact] void should_name_the_environment() => _exception.Message.ShouldContain("Production");
    [Fact] void should_create_no_keys() => Directory.Exists(_storeDirectory).ShouldBeFalse();
    [Fact] void should_not_change_the_secret_store() => _startupOptions.SecretStore.IsConfigured.ShouldBeFalse();
}

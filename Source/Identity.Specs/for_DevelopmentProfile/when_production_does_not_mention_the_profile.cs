// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_production_does_not_mention_the_profile : given.a_development_profile
{
    void Establish()
    {
        _environment.EnvironmentName = "Production";
        _settings.Remove("Identity:Development:Enabled");
    }

    void Because() => Apply();

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_not_apply_the_profile() => _dataProtectionPath.ShouldBeNull();
}

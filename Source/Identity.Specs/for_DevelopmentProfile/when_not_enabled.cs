// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Configuration;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_not_enabled : given.a_development_profile
{
    void Establish() => _settings["Identity:Development:Enabled"] = "false";

    void Because() => Apply();

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_not_apply_the_profile() => _dataProtectionPath.ShouldBeNull();
    [Fact] void should_create_no_keys() => Directory.Exists(Path.Combine(_root, "cratis")).ShouldBeFalse();
    [Fact] void should_leave_the_resources() => _startupOptions.Resources.ShouldBeEmpty();
    [Fact] void should_register_nothing() => _services.Any(_ => _.ServiceType == typeof(DevelopmentOptions)).ShouldBeFalse();
}

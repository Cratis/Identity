// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;

namespace Cratis.Identity.for_DevelopmentProfile;

public class when_a_resource_indicator_is_not_loopback : given.a_development_profile
{
    void Establish() => _settings["Identity:Development:Resources:0:Indicators:0"] = "https://mcp.example.com/mcp";

    void Because() => Apply();

    [Fact] void should_refuse_it() => _exception.ShouldBeOfExactType<DevelopmentProfileRefused>();
}

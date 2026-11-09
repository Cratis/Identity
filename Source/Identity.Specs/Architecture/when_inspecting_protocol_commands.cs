// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc;
using Cratis.Identity.Protocol;

namespace Cratis.Identity.Architecture;

public class when_inspecting_protocol_commands : Specification
{
    [Fact]
    void should_exclude_every_protocol_command_from_http_discovery() =>
        typeof(ProtocolCommandAttribute).Assembly.GetTypes()
            .Where(type => type.IsDefined(typeof(ProtocolCommandAttribute)))
            .All(type => type.IsDefined(typeof(ExcludeFromDiscoveryAttribute)))
            .ShouldBeTrue();
}

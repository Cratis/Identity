// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;

namespace Cratis.Identity.Protocol.for_ProtocolCommandFilter.when_executing_a_protocol_command;

public class and_inside_the_protocol_scope : Specification
{
    CommandResult _result;

    async Task Because()
    {
        using var scope = ProtocolCommandScope.Begin();
        _result = await new ProtocolCommandFilter().OnExecution(new(CorrelationId.New(), typeof(and_outside_the_protocol_scope.ProtocolOnly), new and_outside_the_protocol_scope.ProtocolOnly(), [], new()));
    }

    [Fact] void should_pass_through() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_restore_the_protocol_scope() => ProtocolCommandScope.IsActive.ShouldBeFalse();
}

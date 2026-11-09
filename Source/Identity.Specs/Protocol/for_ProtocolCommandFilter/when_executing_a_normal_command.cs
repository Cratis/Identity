// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;

namespace Cratis.Identity.Protocol.for_ProtocolCommandFilter;

public class when_executing_a_normal_command : Specification
{
    CommandResult _result;

    async Task Because() => _result = await new ProtocolCommandFilter().OnExecution(new(CorrelationId.New(), typeof(object), new object(), [], new()));

    [Fact] void should_pass_through() => _result.IsSuccess.ShouldBeTrue();
}

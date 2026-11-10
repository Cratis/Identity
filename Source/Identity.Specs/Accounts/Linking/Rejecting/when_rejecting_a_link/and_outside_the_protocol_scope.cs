// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;
using Cratis.Identity.Protocol;

namespace Cratis.Identity.Accounts.Linking.Rejecting.when_rejecting_a_link;

public class and_outside_the_protocol_scope : Specification
{
    CommandResult _result;

    async Task Because() => _result = await new ProtocolCommandFilter().OnExecution(new(CorrelationId.New(), typeof(RejectLink), new RejectLink(Guid.NewGuid()), [], new()));

    [Fact] void should_refuse_execution() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_report_the_closed_reason() => _result.AuthorizationFailureReason.ShouldEqual("protocol_only");
}

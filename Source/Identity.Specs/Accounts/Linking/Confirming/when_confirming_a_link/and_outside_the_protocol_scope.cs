// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Cratis.Identity.Accounts.Linking.Clearing;
using Cratis.Identity.Protocol;

namespace Cratis.Identity.Accounts.Linking.Confirming.when_confirming_a_link;

public class and_outside_the_protocol_scope : Specification
{
    [Theory]
    [InlineData(typeof(ConfirmLink))]
    [InlineData(typeof(ClearLinkQuarantine))]
    async Task should_refuse_public_execution(Type commandType)
    {
        var result = await new ProtocolCommandFilter().OnExecution(new(CorrelationId.New(), commandType, new object(), [], new()));
        result.IsAuthorized.ShouldBeFalse();
        result.AuthorizationFailureReason.ShouldEqual("protocol_only");
    }
}

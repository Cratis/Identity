// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_BrowserSessionValidator;

public class when_the_store_fails : given.a_browser_session
{
    void Establish() => _sessions.Find(_sessionId, Arg.Any<CancellationToken>()).Returns<Task<Accounts.BrowserSession?>>(_ => throw new InvalidOperationException("down"));

    async Task Because() => _result = await _validator.IsValid(_principal, CancellationToken.None);

    [Fact] void should_refuse_the_session() => _result.ShouldBeFalse();
}

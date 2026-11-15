// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_BrowserSessionValidator;

public class when_the_session_was_revoked : given.a_browser_session
{
    void Establish() => Recorded(DateTimeOffset.UnixEpoch);

    async Task Because() => _result = await _validator.IsValid(_principal, CancellationToken.None);

    [Fact] void should_refuse_the_session() => _result.ShouldBeFalse();
}

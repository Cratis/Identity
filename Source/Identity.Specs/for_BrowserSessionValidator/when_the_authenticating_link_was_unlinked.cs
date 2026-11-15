// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.for_RefreshGrantAdmission.given;

namespace Cratis.Identity.for_BrowserSessionValidator;

public class when_the_authenticating_link_was_unlinked : given.a_browser_session
{
    void Establish()
    {
        Recorded();
        _accounts.FindActive(Arg.Any<AccountSubject>(), Arg.Any<CancellationToken>()).Returns(a_refresh_admission.AccountWithLinks(a_refresh_admission.GitHubLink));
    }

    async Task Because() => _result = await _validator.IsValid(_principal, CancellationToken.None);

    [Fact] void should_refuse_the_session() => _result.ShouldBeFalse();
}

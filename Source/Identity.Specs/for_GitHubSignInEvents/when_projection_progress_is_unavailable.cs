// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Identity.for_GitHubSignInEvents;

public class when_projection_progress_is_unavailable : Specification
{
    RemoteFailureContext _context;

    async Task Because()
    {
        _context = new(new DefaultHttpContext(), new("GitHub", "GitHub", typeof(OAuthHandler<OAuthOptions>)), new(), new AuthenticationFailureException("Upstream admission failed.", new AccountOperationRefused("temporarily_unavailable")))
        {
            Properties = new() { RedirectUri = "/connect/authorize?client_id=cratis-cli" }
        };
        await new GitHubSignInEvents(Substitute.For<IAccountSignIn>(), NullLogger<GitHubSignInEvents>.Instance, TimeProvider.System).RemoteFailure(_context);
    }

    [Fact] void should_return_only_the_defined_temporary_failure() => _context.Response.Headers.Location.ToString().ShouldEqual("/connect/authorize?client_id=cratis-cli&upstream_error=temporarily_unavailable");
    [Fact] void should_not_issue_a_session_cookie() => _context.Response.Headers.ContainsKey("Set-Cookie").ShouldBeFalse();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Identity.for_GitHubSignInEvents;

public class when_the_request_is_aborted_before_the_callback_fails : Specification
{
    const string ReturnUrl = "/connect/authorize?client_id=cratis-cli";

    CancellationTokenSource _aborted;
    DefaultHttpContext _http;
    RemoteFailureContext _context;

    void Establish()
    {
        _aborted = new();
        _http = new() { RequestAborted = _aborted.Token };
        _context = new RemoteFailureContext(
            _http,
            new AuthenticationScheme("GitHub", "GitHub", typeof(OAuthHandler<OAuthOptions>)),
            new OAuthOptions(),
            new OperationCanceledException(_aborted.Token))
        {
            Properties = new AuthenticationProperties { RedirectUri = ReturnUrl }
        };
        _aborted.Cancel();
    }

    Task Because() => new GitHubSignInEvents(Substitute.For<IAccountSignIn>(), NullLogger<GitHubSignInEvents>.Instance, TimeProvider.System).RemoteFailure(_context);

    void Destroy() => _aborted.Dispose();

    [Fact] void should_handle_the_response_itself() => _context.Result.Handled.ShouldBeTrue();
    [Fact] void should_not_redirect_with_a_denial() => _http.Response.Headers.Location.ToString().ShouldEqual(string.Empty);
    [Fact] void should_not_write_an_error_status() => _http.Response.StatusCode.ShouldEqual(StatusCodes.Status200OK);
}

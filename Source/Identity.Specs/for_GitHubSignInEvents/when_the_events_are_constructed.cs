// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Upstream;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Identity.for_GitHubSignInEvents;

public class when_the_events_are_constructed : Specification
{
    IServiceProvider _services;

    void Establish() => _services = Substitute.For<IServiceProvider>();

    void Because()
    {
        // ASP.NET Core builds a remote authentication handler's Events on every request, not just its own
        // callback, to check whether the request matches its callback path. Resolving IAccountSignIn here
        // would reach Chronicle for unrelated requests, such as the readiness probe, before it has connected.
        _ = new GitHubSignInEvents(_services, NullLogger<GitHubSignInEvents>.Instance, TimeProvider.System);
    }

    [Fact] void should_not_resolve_the_account_sign_in_boundary() => _services.DidNotReceive().GetService(typeof(IAccountSignIn));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Identity.for_EndSessionEndpoint;

public class when_confirming_without_a_hint : given.an_end_session_endpoint
{
    IResult _result;
    int _signOuts;

    async Task Because()
    {
        _result = await endpoint.Handle(Context(post: true));
        _signOuts = SignOutCount();
    }

    [Fact] void should_end_the_confirmed_browser_session() => _signOuts.ShouldEqual(1);
    [Fact] void should_complete_locally_without_a_redirect() => Content(_result).ShouldEqual("Signed out.");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Identity.for_EndSessionEndpoint;

public class when_the_confirmation_has_no_antiforgery_token : given.an_end_session_endpoint
{
    IResult _missing;
    IResult _matching;
    int _signOuts;

    async Task Because()
    {
        antiforgery.IsRequestValidAsync(Arg.Any<HttpContext>()).Returns(false);
        _missing = await endpoint.Handle(Context(post: true));
        _matching = await endpoint.Handle(Context(Sign(), RedirectUri, "rp-state", ClientId, post: true));
        _signOuts = SignOutCount();
    }

    [Fact] void should_show_confirmation_for_an_unprotected_plain_post() => Status(_missing).ShouldEqual(200);
    [Fact] void should_show_confirmation_even_for_a_matching_hint_on_post() => Status(_matching).ShouldEqual(200);
    [Fact] void should_include_a_fresh_antiforgery_field() => Content(_matching).Contains("name=\"__logout_antiforgery\" value=\"form-token\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_preserve_the_validated_redirect_for_confirmation() => Content(_matching).Contains("name=\"post_logout_redirect_uri\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_preserve_state_for_confirmation() => Content(_matching).Contains("value=\"rp-state\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_leave_the_browser_session_intact() => _signOuts.ShouldEqual(0);
}

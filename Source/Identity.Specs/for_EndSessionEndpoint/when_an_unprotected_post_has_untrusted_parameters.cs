// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Identity.for_EndSessionEndpoint;

public class when_an_unprotected_post_has_untrusted_parameters : given.an_end_session_endpoint
{
    IResult _invalidHint;
    IResult _invalidClient;
    IResult _invalidRedirect;
    int _signOuts;

    async Task Because()
    {
        antiforgery.IsRequestValidAsync(Arg.Any<HttpContext>()).Returns(false);
        _invalidHint = await endpoint.Handle(Context(Sign(_ => _["aud"] = "unknown-client"), RedirectUri, post: true));
        _invalidClient = await endpoint.Handle(Context(Sign(), RedirectUri, clientId: "other-client", post: true));
        _invalidRedirect = await endpoint.Handle(Context(Sign(), "https://unregistered.example/signed-out", "discard-state", post: true));
        _signOuts = SignOutCount();
    }

    [Fact] void should_reject_an_invalid_hint_without_redirecting() => Status(_invalidHint).ShouldEqual(400);
    [Fact] void should_reject_a_mismatched_client_without_redirecting() => Status(_invalidClient).ShouldEqual(400);
    [Fact] void should_show_local_confirmation_for_an_unregistered_redirect() => Status(_invalidRedirect).ShouldEqual(400);
    [Fact] void should_not_preserve_an_unregistered_redirect() => Content(_invalidRedirect).Contains("name=\"post_logout_redirect_uri\"", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_discard_state_with_the_invalid_redirect() => Content(_invalidRedirect).Contains("discard-state", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_never_sign_out_without_confirmation() => _signOuts.ShouldEqual(0);
}

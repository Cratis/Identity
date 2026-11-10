// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.WebUtilities;

namespace Cratis.Identity.for_EndSessionEndpoint;

public class when_the_hint_matches_the_browser_session : given.an_end_session_endpoint
{
    IResult _result;
    HttpContext _context;
    int _signOuts;
    const string State = "state<&\"+?= value";

    async Task Because()
    {
        _context = Context(Sign(), RedirectUri, State, ClientId);
        _result = await endpoint.Handle(_context);
        _signOuts = SignOutCount();
    }

    [Fact] void should_end_only_one_browser_session() => _signOuts.ShouldEqual(1);
    [Fact] async Task should_sign_out_only_the_identity_cookie() => await authentication.Received(1).SignOutAsync(_context, IdentitySchemes.Session, Arg.Any<AuthenticationProperties>());
    [Fact] void should_redirect_to_the_exact_registered_uri_with_encoded_state() => ((RedirectHttpResult)_result).Url.ShouldEqual(QueryHelpers.AddQueryString(RedirectUri, "state", State));
    [Fact] void should_not_cache_the_response() => _context.Response.Headers.CacheControl.ToString().ShouldEqual("no-store");
    [Fact] void should_not_send_a_referrer_to_the_relying_party() => _context.Response.Headers["Referrer-Policy"].ToString().ShouldEqual("no-referrer");
    [Fact] void should_not_require_confirmation_when_both_sid_and_subject_match() => antiforgery.ReceivedCalls().ShouldBeEmpty();
}

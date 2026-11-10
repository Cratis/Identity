// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Cratis.Identity.for_EndSessionEndpoint;

public class when_confirmation_is_required : given.an_end_session_endpoint
{
    IResult _missing;
    IResult _noSid;
    IResult _differentSession;
    IResult _differentSubject;
    IResult _noCookie;
    IResult _encoded;
    HttpContext _context;
    int _signOuts;

    async Task Because()
    {
        _missing = await endpoint.Handle(Context());
        _noSid = await endpoint.Handle(Context(Sign(_ => _.Remove("sid"))));
        _differentSession = await endpoint.Handle(Context(Sign(_ => _["sid"] = "other-session")));
        _differentSubject = await endpoint.Handle(Context(Sign(_ => _["sub"] = "other-account")));
        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), Arg.Any<string>()).Returns(AuthenticateResult.NoResult());
        _noCookie = await endpoint.Handle(Context(Sign()));
        _context = Context(Sign(), RedirectUri, "\"><script>alert(1)</script>");
        _encoded = await endpoint.Handle(_context);
        _signOuts = SignOutCount();
    }

    [Fact] void should_confirm_without_a_hint() => Content(_missing).Contains("<form method=\"post\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_confirm_an_old_id_token_without_sid() => Status(_noSid).ShouldEqual(200);
    [Fact] void should_confirm_a_different_session_identifier() => Status(_differentSession).ShouldEqual(200);
    [Fact] void should_confirm_a_different_subject_even_when_sid_matches() => Status(_differentSubject).ShouldEqual(200);
    [Fact] void should_confirm_when_no_cookie_exists() => Status(_noCookie).ShouldEqual(200);
    [Fact] void should_leave_the_cookie_intact_before_confirmation() => _signOuts.ShouldEqual(0);
    [Fact] void should_include_an_antiforgery_field() => Content(_missing).Contains("name=\"__logout_antiforgery\" value=\"form-token\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_encode_hidden_state() => Content(_encoded).Contains("&lt;script&gt;", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_reflect_script_markup() => Content(_encoded).Contains("<script>", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_forbid_framing_and_external_scripts() => _context.Response.Headers.ContentSecurityPolicy.ToString().Contains("default-src 'none'", StringComparison.Ordinal).ShouldBeTrue();
}

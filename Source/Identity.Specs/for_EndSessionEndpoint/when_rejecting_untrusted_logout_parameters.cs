// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Identity.for_EndSessionEndpoint;

public class when_rejecting_untrusted_logout_parameters : given.an_end_session_endpoint
{
    IResult _invalidHint;
    IResult _noHintRedirect;
    IResult _wrongClient;
    IResult _unknownClient;
    IResult _wrongRedirect;
    IResult _fragment;
    IResult _canonicalized;
    IResult _duplicate;
    int _signOuts;

    async Task Because()
    {
        _invalidHint = await endpoint.Handle(Context("not-an-id-token", RedirectUri));
        _noHintRedirect = await endpoint.Handle(Context(redirect: RedirectUri));
        _wrongClient = await endpoint.Handle(Context(Sign(), RedirectUri, clientId: "other-client"));
        _unknownClient = await endpoint.Handle(Context(clientId: "unknown-client"));
        _wrongRedirect = await endpoint.Handle(Context(Sign(), "https://attacker.example/signed-out", "do-not-reflect"));
        _fragment = await endpoint.Handle(Context(Sign(), RedirectUri + "#fragment"));
        _canonicalized = await endpoint.Handle(Context(Sign(), "https://RELYING-PARTY.example/signed-out?existing=value"));
        var duplicate = Context(Sign(), RedirectUri);
        duplicate.Request.QueryString = duplicate.Request.QueryString.Add("post_logout_redirect_uri", RedirectUri);
        _duplicate = await endpoint.Handle(duplicate);
        _signOuts = SignOutCount();
    }

    [Fact] void should_refuse_an_invalid_hint_without_redirecting() => Status(_invalidHint).ShouldEqual(400);
    [Fact] void should_refuse_a_redirect_without_a_validated_hint() => Status(_noHintRedirect).ShouldEqual(400);
    [Fact] void should_refuse_a_client_identifier_that_does_not_match_the_audience() => Status(_wrongClient).ShouldEqual(400);
    [Fact] void should_refuse_an_unknown_client_even_without_a_hint() => Status(_unknownClient).ShouldEqual(400);
    [Fact] void should_offer_only_local_confirmation_for_an_unregistered_redirect() => Content(_wrongRedirect).Contains("<form", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_retain_an_unregistered_redirect() => Content(_wrongRedirect).Contains("attacker.example", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_retain_state_for_a_refused_redirect() => Content(_wrongRedirect).Contains("do-not-reflect", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_refuse_redirect_fragments() => Status(_fragment).ShouldEqual(400);
    [Fact] void should_compare_redirects_ordinally_without_normalization() => Status(_canonicalized).ShouldEqual(400);
    [Fact] void should_refuse_duplicate_redirect_parameters() => Status(_duplicate).ShouldEqual(400);
    [Fact] void should_leave_the_browser_session_intact() => _signOuts.ShouldEqual(0);
}

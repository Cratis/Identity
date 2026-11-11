// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.given;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using OpenIddict.Abstractions;

namespace Cratis.Identity.for_OidcAuthenticationProof;

public class when_returning_from_consent : for_OidcIdentity.given.an_identity_grant
{
    FixedTimeProvider _clock;
    OidcAuthenticationProof _proof;
    SignedInAccount _authentication;
    HttpContext _valid;
    HttpContext _changed;
    HttpContext _tampered;
    bool _acceptedBeforeExpiry;
    bool _refusedAfterExpiry;
    bool _changedRefused;
    bool _tamperedRefused;

    void Establish()
    {
        _clock = new(AuthTime);
        _authentication = new(_account, AuthTime, SessionId);
        _proof = new(new EphemeralDataProtectionProvider(), _clock);
        var initial = new DefaultHttpContext();
        initial.Request.QueryString = new QueryString("?scope=openid&nonce=original&max_age=0");
        var proof = _proof.Issue(initial, _authentication);
        _valid = Form("original", proof);
        _changed = Form("different", proof);
        _tampered = Form("original", "invalid-protected-value");
        _clock.Now = AuthTime.AddSeconds(10);
    }

    void Because()
    {
        _acceptedBeforeExpiry = _proof.IsValid(_valid, _authentication);
        _changedRefused = !_proof.IsValid(_changed, _authentication);
        _tamperedRefused = !_proof.IsValid(_tampered, _authentication);
        _clock.Now = AuthTime.AddMinutes(6);
        _refusedAfterExpiry = !_proof.IsValid(_valid, _authentication);
    }

    [Fact] void should_accept_the_original_request_after_interactive_consent() => _acceptedBeforeExpiry.ShouldBeTrue();
    [Fact] void should_not_reinterpret_a_stale_cookie_as_new_authentication() => OidcAuthentication.IsFresh(new OpenIddictRequest { Scope = "openid", MaxAge = 0 }, AuthTime, _clock.GetUtcNow()).ShouldBeFalse();
    [Fact] void should_not_accept_a_changed_nonce() => _changedRefused.ShouldBeTrue();
    [Fact] void should_not_accept_a_tampered_binding() => _tamperedRefused.ShouldBeTrue();
    [Fact] void should_expire_the_original_request_binding() => _refusedAfterExpiry.ShouldBeTrue();

    static HttpContext Form(string nonce, string proof)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.Form = new FormCollection(new Dictionary<string, StringValues>
        {
            ["scope"] = "openid", ["nonce"] = nonce, ["max_age"] = "0",
            [OidcAuthenticationProof.Field] = proof,
            [ConsentForm.Decision] = ConsentForm.Allow,
            [ConsentForm.AntiforgeryField] = "example-antiforgery-token"
        });
        return context;
    }
}

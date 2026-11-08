// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.given;

namespace Cratis.Identity.for_SubjectTokenValidator;

public class when_validating_valid_tokens : given.a_subject_token_validator
{
    SubjectTokenResult _rsa;
    SubjectTokenResult _ec;
    SubjectTokenResult _withinSkew;
    SubjectTokenResult _withoutAuthTime;
    SubjectTokenResult _withAtJwtType;

    async Task Because()
    {
        _rsa = await Validate(Valid());
        _ec = await Validate(ExternalIssuerFixture.Sign(Payload(), issuer.EcCredentials));

        var expired = Payload();
        expired["exp"] = time.Now.AddSeconds(-30).ToUnixTimeSeconds();
        _withinSkew = await Validate(ExternalIssuerFixture.Sign(expired, issuer.RsaCredentials));

        var noAuthTime = Payload();
        noAuthTime.Remove("auth_time");
        _withoutAuthTime = await Validate(ExternalIssuerFixture.Sign(noAuthTime, issuer.RsaCredentials));

        _withAtJwtType = await Validate(ExternalIssuerFixture.Sign(Payload(), issuer.RsaCredentials, new() { ["typ"] = "at+jwt" }));
    }

    [Fact] void should_accept_an_rs256_token() => _rsa.Refusal.ShouldEqual(SubjectTokenRefusal.None);
    [Fact] void should_accept_an_es256_token() => _ec.Refusal.ShouldEqual(SubjectTokenRefusal.None);
    [Fact] void should_accept_a_token_expired_within_the_skew() => _withinSkew.Refusal.ShouldEqual(SubjectTokenRefusal.None);
    [Fact] void should_accept_a_token_without_an_authentication_time() => _withoutAuthTime.Refusal.ShouldEqual(SubjectTokenRefusal.None);
    [Fact] void should_accept_an_access_token_jwt_type() => _withAtJwtType.Refusal.ShouldEqual(SubjectTokenRefusal.None);
    [Fact] void should_return_the_configured_issuer() => _rsa.Subject.Issuer.Value.ShouldEqual(Configurations.ExternalIssuer);
    [Fact] void should_return_the_subject() => _rsa.Subject.Subject.Value.ShouldEqual("external-subject-1");
    [Fact] void should_return_the_authentication_time() => _rsa.Subject.AuthTime.ShouldEqual(time.Now.AddSeconds(-30).ToUnixTimeSeconds());
    [Fact] void should_not_invent_an_authentication_time() => _withoutAuthTime.Subject.AuthTime.ShouldBeNull();
    [Fact] void should_return_the_expiry() => _rsa.Subject.Expiry.ShouldEqual(DateTimeOffset.FromUnixTimeSeconds(time.Now.AddMinutes(5).ToUnixTimeSeconds()));
}

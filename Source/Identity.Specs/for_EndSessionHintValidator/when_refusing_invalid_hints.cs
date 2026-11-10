// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Identity.given;
using Cratis.Identity.Sessions;

namespace Cratis.Identity.for_EndSessionHintValidator;

public class when_refusing_invalid_hints : given.a_hint_validator
{
    readonly Dictionary<string, bool> _refused = new(StringComparer.Ordinal);

    async Task Because()
    {
        _refused["issuer"] = await Refused(Sign(_ => _["iss"] = "https://other.example/"));
        _refused["audience"] = await Refused(Sign(_ => _["aud"] = "unknown-client"));
        _refused["missing-audience"] = await Refused(Sign(_ => _.Remove("aud")));
        _refused["multiple"] = await Refused(Sign(_ => _["aud"] = new[] { ClientId, "other-client" }));
        _refused["azp"] = await Refused(Sign(_ => _["azp"] = "other-client"));
        _refused["old"] = await Refused(Sign(_ => _["iat"] = time.Now.AddDays(-2).ToUnixTimeSeconds()));
        _refused["future"] = await Refused(Sign(_ => _["iat"] = time.Now.AddMinutes(1).ToUnixTimeSeconds()));
        _refused["nbf"] = await Refused(Sign(_ => _["nbf"] = time.Now.AddMinutes(1).ToUnixTimeSeconds()));
        _refused["order"] = await Refused(Sign(_ => _["exp"] = time.Now.AddHours(-1).ToUnixTimeSeconds()));
        _refused["iat"] = await Refused(Sign(_ => _.Remove("iat")));
        _refused["exp"] = await Refused(Sign(_ => _.Remove("exp")));
        _refused["subject"] = await Refused(Sign(_ => _["sub"] = string.Empty));
        _refused["missing-subject"] = await Refused(Sign(_ => _.Remove("sub")));
        _refused["marker"] = await Refused(Sign(_ => _.Remove("auth_time")));
        _refused["scope"] = await Refused(Sign(_ => _["scope"] = "openid"));
        _refused["cid"] = await Refused(Sign(_ => _["cid"] = "consent-id"));
        _refused["refresh"] = await Refused(Sign(_ => _["oi_tkn_typ"] = "refresh_token"));
        _refused["size"] = await Refused(new string('x', EndSessionHintValidator.MaximumHintLength + 1));
        _refused["opaque"] = await Refused("opaque-refresh-token");
        _refused["jwe"] = await Refused("one.two.three.four.five");
        _refused["none"] = await Refused(ExternalIssuerFixture.AlgNone(Payload()));
        _refused["hmac"] = await Refused(keys.HmacWithPublicKey(Payload()));
        _refused["ec"] = await Refused(ExternalIssuerFixture.Sign(Payload(), keys.EcCredentials));
        _refused["signature"] = await Refused(ExternalIssuerFixture.SignWithUnpublishedKey(Payload()));
        _refused["kid"] = await Refused(WithHeader(new() { ["typ"] = "JWT", ["alg"] = "RS256", ["kid"] = "unknown-key" }));
        _refused["missing-kid"] = await Refused(WithHeader(new() { ["typ"] = "JWT", ["alg"] = "RS256" }));
        _refused["type"] = await Refused(WithHeader(new() { ["typ"] = "at+jwt", ["alg"] = "RS256", ["kid"] = ExternalIssuerFixture.RsaKeyId }));
        _refused["missing-type"] = await Refused(WithHeader(new() { ["alg"] = "RS256", ["kid"] = ExternalIssuerFixture.RsaKeyId }));
        applications.GetPermissionsAsync(application, Arg.Any<CancellationToken>()).Returns(ImmutableArray<string>.Empty);
        _refused["non-oidc"] = await Refused(Sign());
    }

    [Fact] void should_refuse_a_different_realm_issuer() => _refused["issuer"].ShouldBeTrue();
    [Fact] void should_refuse_an_unknown_relying_party() => _refused["audience"].ShouldBeTrue();
    [Fact] void should_require_a_relying_party_audience() => _refused["missing-audience"].ShouldBeTrue();
    [Fact] void should_refuse_multiple_audiences() => _refused["multiple"].ShouldBeTrue();
    [Fact] void should_refuse_a_mismatched_authorized_party() => _refused["azp"].ShouldBeTrue();
    [Fact] void should_refuse_a_hint_older_than_the_age_limit() => _refused["old"].ShouldBeTrue();
    [Fact] void should_refuse_a_future_issuance_time() => _refused["future"].ShouldBeTrue();
    [Fact] void should_refuse_a_future_not_before_time() => _refused["nbf"].ShouldBeTrue();
    [Fact] void should_refuse_expiry_before_issuance() => _refused["order"].ShouldBeTrue();
    [Fact] void should_require_an_issuance_time() => _refused["iat"].ShouldBeTrue();
    [Fact] void should_require_an_expiry() => _refused["exp"].ShouldBeTrue();
    [Fact] void should_refuse_an_empty_subject() => _refused["subject"].ShouldBeTrue();
    [Fact] void should_require_a_subject() => _refused["missing-subject"].ShouldBeTrue();
    [Fact] void should_require_an_id_token_marker() => _refused["marker"].ShouldBeTrue();
    [Fact] void should_refuse_an_access_token_scope_marker() => _refused["scope"].ShouldBeTrue();
    [Fact] void should_refuse_an_access_token_consent_marker() => _refused["cid"].ShouldBeTrue();
    [Fact] void should_refuse_a_refresh_token_in_jwt_form() => _refused["refresh"].ShouldBeTrue();
    [Fact] void should_refuse_oversized_hints() => _refused["size"].ShouldBeTrue();
    [Fact] void should_refuse_opaque_tokens() => _refused["opaque"].ShouldBeTrue();
    [Fact] void should_refuse_encrypted_tokens() => _refused["jwe"].ShouldBeTrue();
    [Fact] void should_refuse_unsigned_tokens() => _refused["none"].ShouldBeTrue();
    [Fact] void should_refuse_hmac_algorithm_confusion() => _refused["hmac"].ShouldBeTrue();
    [Fact] void should_refuse_ec_signatures() => _refused["ec"].ShouldBeTrue();
    [Fact] void should_refuse_a_forged_signature() => _refused["signature"].ShouldBeTrue();
    [Fact] void should_refuse_unknown_key_identifiers() => _refused["kid"].ShouldBeTrue();
    [Fact] void should_require_a_key_identifier() => _refused["missing-kid"].ShouldBeTrue();
    [Fact] void should_refuse_access_token_types() => _refused["type"].ShouldBeTrue();
    [Fact] void should_require_the_jwt_type() => _refused["missing-type"].ShouldBeTrue();
    [Fact] void should_refuse_a_registered_client_without_openid() => _refused["non-oidc"].ShouldBeTrue();
}

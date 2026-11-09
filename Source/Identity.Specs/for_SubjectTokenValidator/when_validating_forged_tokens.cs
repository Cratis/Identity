// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.given;

namespace Cratis.Identity.for_SubjectTokenValidator;

public class when_validating_forged_tokens : given.a_subject_token_validator
{
    SubjectTokenRefusal _none;
    SubjectTokenRefusal _hmacWithPublicKey;
    SubjectTokenRefusal _unpublishedKey;
    SubjectTokenRefusal _tampered;
    SubjectTokenRefusal _unexpectedType;
    SubjectTokenRefusal _disallowedAlgorithm;

    async Task Because()
    {
        _none = await RefusalOf(ExternalIssuerFixture.AlgNone(Payload()));
        _hmacWithPublicKey = await RefusalOf(issuer.HmacWithPublicKey(Payload()));
        _unpublishedKey = await RefusalOf(ExternalIssuerFixture.SignWithUnpublishedKey(Payload()));

        var parts = Valid().Split('.');
        var changed = Payload();
        changed["sub"] = "someone-else";
        var forgedPayload = ExternalIssuerFixture.Sign(changed, issuer.RsaCredentials).Split('.')[1];
        _tampered = await RefusalOf($"{parts[0]}.{forgedPayload}.{parts[2]}");

        _unexpectedType = await RefusalOf(ExternalIssuerFixture.Sign(Payload(), issuer.RsaCredentials, new() { ["typ"] = "secevent+jwt" }));

        IssuerOptions.SigningAlgorithms = ["ES256"];
        _disallowedAlgorithm = await RefusalOf(Valid());
    }

    [Fact] void should_refuse_alg_none() => _none.ShouldEqual(SubjectTokenRefusal.Invalid);
    [Fact] void should_refuse_hs256_keyed_with_the_public_key() => _hmacWithPublicKey.ShouldEqual(SubjectTokenRefusal.Invalid);
    [Fact] void should_refuse_a_token_signed_by_an_unpublished_key() => _unpublishedKey.ShouldEqual(SubjectTokenRefusal.Invalid);
    [Fact] void should_refuse_a_tampered_payload() => _tampered.ShouldEqual(SubjectTokenRefusal.Invalid);
    [Fact] void should_refuse_an_unexpected_token_type() => _unexpectedType.ShouldEqual(SubjectTokenRefusal.Invalid);
    [Fact] void should_refuse_an_algorithm_outside_the_allowlist() => _disallowedAlgorithm.ShouldEqual(SubjectTokenRefusal.Invalid);
}

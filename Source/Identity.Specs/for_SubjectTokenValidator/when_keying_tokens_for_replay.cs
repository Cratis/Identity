// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.given;

namespace Cratis.Identity.for_SubjectTokenValidator;

public class when_keying_tokens_for_replay : given.a_subject_token_validator
{
    SubjectTokenResult _withJti;
    SubjectTokenResult _withJtiResigned;
    SubjectTokenResult _otherJti;
    SubjectTokenResult _sameJtiOtherSubject;
    SubjectTokenResult _withoutJti;
    SubjectTokenResult _withoutJtiResigned;
    SubjectTokenResult _withoutJtiOtherExpiry;

    async Task Because()
    {
        // ECDSA signatures are randomized: the same claims signed twice are two different compact strings.
        var payload = Payload();
        _withJti = await Validate(ExternalIssuerFixture.Sign(payload, issuer.EcCredentials));
        _withJtiResigned = await Validate(ExternalIssuerFixture.Sign(payload, issuer.EcCredentials));

        var other = Payload();
        other["jti"] = "a-different-token";
        _otherJti = await Validate(ExternalIssuerFixture.Sign(other, issuer.EcCredentials));

        var otherSubject = new Dictionary<string, object>(payload) { ["sub"] = "someone-else" };
        _sameJtiOtherSubject = await Validate(ExternalIssuerFixture.Sign(otherSubject, issuer.EcCredentials));

        var bare = Payload();
        bare.Remove("jti");
        _withoutJti = await Validate(ExternalIssuerFixture.Sign(bare, issuer.EcCredentials));
        _withoutJtiResigned = await Validate(ExternalIssuerFixture.Sign(bare, issuer.EcCredentials));

        var laterExpiry = new Dictionary<string, object>(bare) { ["exp"] = time.Now.AddMinutes(6).ToUnixTimeSeconds() };
        _withoutJtiOtherExpiry = await Validate(ExternalIssuerFixture.Sign(laterExpiry, issuer.EcCredentials));
    }

    [Fact] void should_give_the_same_key_to_the_same_claims_signed_again() => _withJtiResigned.Subject.ReplayKey.ShouldEqual(_withJti.Subject.ReplayKey);
    [Fact] void should_not_collapse_tokens_with_different_ids() => _otherJti.Subject.ReplayKey.ShouldNotEqual(_withJti.Subject.ReplayKey);
    [Fact] void should_not_collapse_the_same_id_for_different_subjects() => _sameJtiOtherSubject.Subject.ReplayKey.ShouldNotEqual(_withJti.Subject.ReplayKey);
    [Fact] void should_give_the_same_key_without_an_id_to_the_same_claims_signed_again() => _withoutJtiResigned.Subject.ReplayKey.ShouldEqual(_withoutJti.Subject.ReplayKey);
    [Fact] void should_detect_a_replay_without_an_id() => RecordedTwice(_withoutJti, _withoutJtiResigned).ShouldBeFalse();
    [Fact] void should_detect_a_replay_with_an_id() => RecordedTwice(_withJti, _withJtiResigned).ShouldBeFalse();
    [Fact] void should_not_collapse_tokens_without_an_id_that_expire_differently() => _withoutJtiOtherExpiry.Subject.ReplayKey.ShouldNotEqual(_withoutJti.Subject.ReplayKey);

    bool RecordedTwice(SubjectTokenResult first, SubjectTokenResult second)
    {
        replay.TryRecord(first.Subject.ReplayKey, time.Now.AddMinutes(5));
        return replay.TryRecord(second.Subject.ReplayKey, time.Now.AddMinutes(5));
    }
}

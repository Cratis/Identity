// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.given;

namespace Cratis.Identity.for_SubjectTokenValidator;

public class when_validating_claims_and_lifetime : given.a_subject_token_validator
{
    SubjectTokenRefusal _wrongAudience;
    SubjectTokenRefusal _noAudience;
    SubjectTokenRefusal _expired;
    SubjectTokenRefusal _noExpiry;
    SubjectTokenRefusal _notYetValid;
    SubjectTokenRefusal _noSubject;
    SubjectTokenRefusal _noIssuedAt;
    SubjectTokenRefusal _tooOld;
    SubjectTokenRefusal _issuedInTheFuture;
    SubjectTokenRefusal _oversized;
    SubjectTokenRefusal _notAJwt;
    SubjectTokenRefusal _empty;

    async Task Because()
    {
        _wrongAudience = await RefusalOf(Sign(_ => _["aud"] = "https://elsewhere.example"));
        _noAudience = await RefusalOf(Sign(_ => _.Remove("aud")));
        _expired = await RefusalOf(Sign(_ => _["exp"] = time.Now.AddMinutes(-5).ToUnixTimeSeconds()));
        _noExpiry = await RefusalOf(Sign(_ => _.Remove("exp")));
        _notYetValid = await RefusalOf(Sign(_ => _["nbf"] = time.Now.AddMinutes(5).ToUnixTimeSeconds()));
        _noSubject = await RefusalOf(Sign(_ => _.Remove("sub")));
        _noIssuedAt = await RefusalOf(Sign(_ => _.Remove("iat")));
        _tooOld = await RefusalOf(Sign(_ => _["iat"] = time.Now.AddMinutes(-11).ToUnixTimeSeconds()));
        _issuedInTheFuture = await RefusalOf(Sign(_ => _["iat"] = time.Now.AddMinutes(3).ToUnixTimeSeconds()));
        _oversized = await RefusalOf(Sign(_ => _["pad"] = new string('x', SubjectTokenValidator.MaximumTokenLength)));
        _notAJwt = await RefusalOf("not.a.jwt");
        _empty = await RefusalOf(string.Empty);
    }

    string Sign(Action<Dictionary<string, object>> change)
    {
        var payload = Payload();
        change(payload);
        return ExternalIssuerFixture.Sign(payload, issuer.RsaCredentials);
    }

    [Fact] void should_refuse_a_wrong_audience() => _wrongAudience.ShouldEqual(SubjectTokenRefusal.Invalid);
    [Fact] void should_refuse_a_missing_audience() => _noAudience.ShouldEqual(SubjectTokenRefusal.Invalid);
    [Fact] void should_refuse_an_expired_token() => _expired.ShouldEqual(SubjectTokenRefusal.Invalid);
    [Fact] void should_refuse_a_token_without_expiry() => _noExpiry.ShouldEqual(SubjectTokenRefusal.Invalid);
    [Fact] void should_refuse_a_token_not_yet_valid() => _notYetValid.ShouldEqual(SubjectTokenRefusal.Invalid);
    [Fact] void should_refuse_a_token_without_subject() => _noSubject.ShouldEqual(SubjectTokenRefusal.MissingClaim);
    [Fact] void should_refuse_a_token_without_issued_at() => _noIssuedAt.ShouldEqual(SubjectTokenRefusal.MissingClaim);
    [Fact] void should_refuse_a_token_older_than_the_maximum_age() => _tooOld.ShouldEqual(SubjectTokenRefusal.Stale);
    [Fact] void should_refuse_a_token_issued_in_the_future() => _issuedInTheFuture.ShouldEqual(SubjectTokenRefusal.Stale);
    [Fact] void should_refuse_an_oversized_token() => _oversized.ShouldEqual(SubjectTokenRefusal.Malformed);
    [Fact] void should_refuse_something_that_is_not_a_jwt() => _notAJwt.ShouldEqual(SubjectTokenRefusal.Malformed);
    [Fact] void should_refuse_an_empty_token() => _empty.ShouldEqual(SubjectTokenRefusal.Malformed);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.Configuration;
using Cratis.Identity.given;

namespace Cratis.Identity.for_SubjectTokenValidator;

public class when_validating_tokens_from_the_wrong_issuer : given.a_subject_token_validator
{
    SubjectTokenRefusal _unknown;
    SubjectTokenRefusal _caseVariant;
    SubjectTokenRefusal _notEnabledForTheClient;
    SubjectTokenRefusal _ownIssuer;

    async Task Because()
    {
        var unknown = Payload();
        unknown["iss"] = "https://stranger.example";
        _unknown = await RefusalOf(ExternalIssuerFixture.Sign(unknown, issuer.RsaCredentials));

        var variant = Payload();
        variant["iss"] = Configurations.ExternalIssuer.ToUpperInvariant();
        _caseVariant = await RefusalOf(ExternalIssuerFixture.Sign(variant, issuer.RsaCredentials));

        var other = new ExternalIssuerOptions { Issuer = "https://other.example", JwksUri = "https://other.example/jwks", Audiences = [Configurations.ExternalAudience] };
        options.TokenExchange.Issuers.Add(other);
        var otherPayload = Payload();
        otherPayload["iss"] = other.Issuer;
        _notEnabledForTheClient = await RefusalOf(ExternalIssuerFixture.Sign(otherPayload, issuer.RsaCredentials));

        var own = Payload();
        own["iss"] = options.Issuer;
        _ownIssuer = await RefusalOf(ExternalIssuerFixture.Sign(own, issuer.RsaCredentials));
    }

    [Fact] void should_refuse_an_unknown_issuer() => _unknown.ShouldEqual(SubjectTokenRefusal.UnknownIssuer);
    [Fact] void should_compare_the_issuer_exactly() => _caseVariant.ShouldEqual(SubjectTokenRefusal.UnknownIssuer);
    [Fact] void should_refuse_an_issuer_the_client_may_not_present() => _notEnabledForTheClient.ShouldEqual(SubjectTokenRefusal.IssuerNotAllowed);
    [Fact] void should_refuse_identitys_own_issuer() => _ownIssuer.ShouldEqual(SubjectTokenRefusal.UnknownIssuer);
    [Fact] void should_not_fetch_keys_for_any_of_them() => keys.ReceivedCalls().ShouldBeEmpty();
}

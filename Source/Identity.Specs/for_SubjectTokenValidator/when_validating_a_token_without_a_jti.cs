// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.given;

namespace Cratis.Identity.for_SubjectTokenValidator;

public class when_validating_a_token_without_a_jti : given.a_subject_token_validator
{
    SubjectTokenResult _first;
    SubjectTokenResult _same;
    SubjectTokenResult _other;

    async Task Because()
    {
        var payload = Payload();
        payload.Remove("jti");
        var token = ExternalIssuerFixture.Sign(payload, issuer.RsaCredentials);
        _first = await Validate(token);
        _same = await Validate(token);
        payload["sub"] = "someone-else";
        _other = await Validate(ExternalIssuerFixture.Sign(payload, issuer.RsaCredentials));
    }

    [Fact] void should_accept_the_token() => _first.Refusal.ShouldEqual(SubjectTokenRefusal.None);
    [Fact] void should_key_it_the_same_for_the_same_token() => _first.Subject.ReplayKey.ShouldEqual(_same.Subject.ReplayKey);
    [Fact] void should_key_a_different_token_differently() => _other.Subject.ReplayKey.ShouldNotEqual(_first.Subject.ReplayKey);
    [Fact] void should_not_put_the_token_in_the_key() => _first.Subject.ReplayKey.StartsWith($"{Configurations.ExternalIssuer}\nsha256:", StringComparison.Ordinal).ShouldBeTrue();
}

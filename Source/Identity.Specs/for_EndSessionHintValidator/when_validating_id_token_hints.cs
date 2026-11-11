// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;
using Cratis.Identity.Sessions;

namespace Cratis.Identity.for_EndSessionHintValidator;

public class when_validating_id_token_hints : given.a_hint_validator
{
    EndSessionHint _live;
    EndSessionHint _expired;
    EndSessionHint _legacy;
    EndSessionHint _nonce;
    EndSessionHint _hash;
    EndSessionHint _overlap;

    async Task Because()
    {
        _live = await validator.Validate(Sign());
        _expired = await validator.Validate(Sign(_ => _["exp"] = time.Now.AddMinutes(-1).ToUnixTimeSeconds()));
        _legacy = await validator.Validate(Sign(_ => _.Remove("sid")));
        _nonce = await validator.Validate(Sign(_ =>
        {
            _.Remove("auth_time");
            _["nonce"] = "nonce-value";
        }));
        _hash = await validator.Validate(Sign(_ =>
        {
            _.Remove("auth_time");
            _["at_hash"] = "hash-value";
        }));
        using var next = new ExternalIssuerFixture();
        var nextCredentials = next.RsaCredentials;
        nextCredentials.Key.KeyId = "next-key";
        server.SigningCredentials.Insert(0, nextCredentials);
        _overlap = await validator.Validate(Sign());
    }

    [Fact] void should_accept_a_live_realm_id_token() => _live.ShouldNotBeNull();
    [Fact] void should_accept_a_recently_expired_id_token() => _expired.ShouldNotBeNull();
    [Fact] void should_keep_the_relying_party() => _live.ClientId.ShouldEqual(ClientId);
    [Fact] void should_keep_the_subject() => _live.Subject.ShouldEqual(Subject);
    [Fact] void should_keep_the_browser_session_identifier() => _live.SessionId.ShouldEqual(SessionId);
    [Fact] void should_accept_an_old_id_token_without_a_session_identifier() => _legacy.SessionId.ShouldBeNull();
    [Fact] void should_accept_a_nonce_as_an_id_token_marker() => _nonce.ShouldNotBeNull();
    [Fact] void should_accept_an_access_token_hash_as_an_id_token_marker() => _hash.ShouldNotBeNull();
    [Fact] void should_accept_a_still_present_overlap_signing_key() => _overlap.ShouldNotBeNull();
}

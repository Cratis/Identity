// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using Cratis.Identity.Revocation.Operational;
using MongoDB.Bson;

namespace Cratis.Identity.for_FenceStoreRules;

public class when_a_stored_document_disagrees_with_its_payload : Specification
{
    GrantState _state;
    bool _otherGrant;
    bool _version;
    bool _fenced;
    bool _token;
    bool _nullToken;
    bool _undelivered;
    bool _unknownPayload;

    void Establish() => _state = FenceStoreFixtures.Clear();

    void Because()
    {
        _otherGrant = Check(_state, "other", d => { });
        _version = Check(_state, FenceStoreFixtures.GrantId, d => d["version"] = 2L);
        _fenced = Check(_state, FenceStoreFixtures.GrantId, d => d["fenced"] = true);
        _token = Check(_state, FenceStoreFixtures.GrantId, d => d["currentRefreshTokenId"] = "other");
        _nullToken = Check(_state, FenceStoreFixtures.GrantId, d => d["currentRefreshTokenId"] = BsonNull.Value);
        _undelivered = Check(_state, FenceStoreFixtures.GrantId, d => d["undelivered"] = false);
        _unknownPayload = Check(_state with { Version = 0 }, FenceStoreFixtures.GrantId, d => d["version"] = 0L);
    }

    [Fact] void should_refuse_a_different_grant() => _otherGrant.ShouldBeFalse();
    [Fact] void should_refuse_a_different_version() => _version.ShouldBeFalse();
    [Fact] void should_refuse_a_different_fenced_flag() => _fenced.ShouldBeFalse();
    [Fact] void should_refuse_a_different_current_token() => _token.ShouldBeFalse();
    [Fact] void should_refuse_a_missing_current_token() => _nullToken.ShouldBeFalse();
    [Fact] void should_refuse_a_different_undelivered_flag() => _undelivered.ShouldBeFalse();
    [Fact] void should_refuse_a_payload_that_is_not_a_known_state() => _unknownPayload.ShouldBeFalse();

    static bool Check(GrantState state, string grantId, Action<BsonDocument> mutate)
    {
        var document = FenceStoreFixtures.StoredClear(state);
        mutate(document);
        return FenceStoreRules.IsConsistent(state, grantId, document);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;
using Cratis.Identity.RevocationSpecs;
using MongoDB.Bson;

namespace Cratis.Identity.for_FenceStoreRules;

internal static class FenceStoreFixtures
{
    internal const string GrantId = "grant";
    internal const string TokenId = "token";

    internal static GrantState Clear(long version = 1, string tokenId = TokenId, long epoch = 0) =>
        new(GrantId, version, tokenId, null, [OutboxEntry.Create(FactKind.FamilyStarted, GrantId, FenceDoubles.Subject, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(30))], epoch);

    internal static GrantFence Fence() => new(Guid.NewGuid(), 1, GrantId, FenceDoubles.Subject, RevocationReason.Revoked, DateTimeOffset.UnixEpoch);

    internal static GrantState Fenced(GrantFence fence, long version = 2) => new(GrantId, version, null, fence, FenceRecordFactory.Entries(fence), 0);

    internal static BsonDocument StoredClear(GrantState state) => new()
    {
        { "_id", state.GrantId }, { "version", state.Version }, { "fenced", false }, { "fenceId", BsonNull.Value },
        { "currentRefreshTokenId", state.CurrentRefreshTokenId }, { "undelivered", true }, { "payload", "{}" }
    };

    internal static BsonDocument StoredFenced(GrantState state) => new()
    {
        { "_id", state.GrantId }, { "version", state.Version }, { "fenced", true }, { "fenceId", state.Fence!.Id.ToString("D") },
        { "currentRefreshTokenId", BsonNull.Value }, { "undelivered", true }, { "payload", "{}" }
    };
}

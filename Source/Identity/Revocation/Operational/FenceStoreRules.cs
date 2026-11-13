// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Identity.Revocation.Operational;

/// <summary>The pure decisions and document shapes behind the refresh/revoke fence, kept free of I/O so they can be pinned without a database.</summary>
internal static class FenceStoreRules
{
    /// <summary>Whether a document that has no payload is only a cancellation that won before the first operational write.</summary>
    /// <param name="document">The stored document.</param>
    /// <returns>Whether it carries at least one cancelled reservation.</returns>
    internal static bool IsCancellationOnly(BsonDocument document) =>
        document.TryGetValue("cancelledReservations", out var cancelled) && cancelled.AsBsonArray.Count != 0;

    /// <summary>Whether the deserialized payload agrees with the indexed columns of its document.</summary>
    /// <param name="state">The deserialized state.</param>
    /// <param name="grantId">The requested grant.</param>
    /// <param name="document">The stored document.</param>
    /// <returns>Whether the payload is known and consistent; false means the read is not provable.</returns>
    internal static bool IsConsistent(GrantState state, string grantId, BsonDocument document) =>
        GrantStateValidation.IsKnown(state) && state.GrantId == grantId && state.Version == document["version"].AsInt64 &&
        (state.Fence is not null) == document["fenced"].AsBoolean &&
        state.CurrentRefreshTokenId == StoredCurrentRefreshTokenId(document) &&
        HasUndelivered(state) == document["undelivered"].AsBoolean;

    /// <summary>Whether the state stops redemption by itself: a permanent restriction.</summary>
    /// <param name="current">The current state.</param>
    /// <returns>Whether a fence is present.</returns>
    internal static bool IsFenced(GrantState current) => current.Fence is not null;

    /// <summary>Whether the state belongs to a different recovery epoch than the log's.</summary>
    /// <param name="current">The current state.</param>
    /// <param name="epoch">The log's recovery epoch.</param>
    /// <returns>Whether the epochs differ.</returns>
    internal static bool IsStaleEpoch(GrantState current, long epoch) => current.RecoveryEpoch != epoch;

    /// <summary>Classifies why a redemption cannot proceed against the state it just read.</summary>
    /// <param name="current">The current, unfenced state.</param>
    /// <param name="consumedId">The presented refresh token.</param>
    /// <param name="expectedVersion">The version the caller validated against.</param>
    /// <returns>The refusal, or null when the successor may be installed.</returns>
    internal static RedeemOutcome? Refusal(GrantState current, string consumedId, long expectedVersion)
    {
        if (current.CurrentRefreshTokenId != consumedId)
        {
            return RedeemOutcome.NotCurrent;
        }

        return current.Version != expectedVersion ? RedeemOutcome.VersionConflict : null;
    }

    /// <summary>Classifies the outcome after the conditional install lost to another writer.</summary>
    /// <param name="winner">The state re-read after the lost write.</param>
    /// <param name="consumedId">The presented refresh token.</param>
    /// <returns>The outcome.</returns>
    internal static RedeemOutcome AfterLostRace(GrantState? winner, string consumedId) => winner switch
    {
        null => RedeemOutcome.Unknown,
        { Fence: not null } => RedeemOutcome.Fenced,
        _ when winner.CurrentRefreshTokenId != consumedId => RedeemOutcome.NotCurrent,
        _ => RedeemOutcome.VersionConflict
    };

    /// <summary>Builds the set payload of a replacement.</summary>
    /// <param name="next">The new authority revision.</param>
    /// <param name="replayEpoch">An optional atomic delivery-reset marker.</param>
    /// <returns>The document.</returns>
    internal static BsonDocument ReplacementDocument(GrantState next, long? replayEpoch)
    {
        var document = new BsonDocument
        {
            { "version", next.Version }, { "fenced", next.Fence is not null },
            { "fenceId", next.Fence is null ? BsonNull.Value : new BsonString(next.Fence.Id.ToString("D")) },
            { "currentRefreshTokenId", TokenIdValue(next.CurrentRefreshTokenId) },
            { "undelivered", HasUndelivered(next) },
            { "payload", JsonSerializer.Serialize(next) }
        };
        if (replayEpoch is not null)
        {
            document.Add("replayEpoch", replayEpoch.Value);
        }

        return document;
    }

    /// <summary>Builds the compare-and-set filter of a replacement.</summary>
    /// <param name="current">The expected authority revision, or null for the first write.</param>
    /// <param name="next">The new authority revision.</param>
    /// <returns>The filter.</returns>
    internal static BsonDocument ReplacementFilter(GrantState? current, GrantState next)
    {
        var filter = current is null
            ? new BsonDocument { { "_id", next.GrantId }, { "payload", new BsonDocument("$exists", false) } }
            : new BsonDocument { { "_id", current.GrantId }, { "version", current.Version }, { "fenced", current.Fence is not null }, { "currentRefreshTokenId", TokenIdValue(current.CurrentRefreshTokenId) } };
        if (next.Fence is { } fence)
        {
            filter.Add("cancelledReservations", new BsonDocument("$ne", fence.Id.ToString("D")));
        }

        return filter;
    }

    /// <summary>Whether the state is the permanent denial the reservation was allocated for.</summary>
    /// <param name="state">The re-read state.</param>
    /// <param name="reservation">The reservation being cancelled.</param>
    /// <returns>Whether the fence carries exactly the reservation's identity and sequence.</returns>
    internal static bool IsCommittedBy(GrantState state, FenceReservation reservation) =>
        state.Fence is { } fence && fence.Id == reservation.Id && fence.Sequence == reservation.Sequence;

    /// <summary>Builds the filter that records a cancellation unless that reservation already committed.</summary>
    /// <param name="reservation">The reservation being cancelled.</param>
    /// <returns>The filter.</returns>
    internal static BsonDocument CancellationFilter(FenceReservation reservation) =>
        new() { { "_id", reservation.GrantId }, { "fenceId", new BsonDocument("$ne", reservation.Id.ToString("D")) } };

    /// <summary>Builds the update that records a cancellation marker.</summary>
    /// <param name="reservation">The reservation being cancelled.</param>
    /// <returns>The update.</returns>
    internal static BsonDocument CancellationUpdate(FenceReservation reservation) =>
        new("$addToSet", new BsonDocument("cancelledReservations", reservation.Id.ToString("D")));

    /// <summary>Whether a conditional write matched or created exactly the intended document.</summary>
    /// <param name="result">The driver result.</param>
    /// <returns>Whether the write is acknowledged and took effect.</returns>
    internal static bool IsCommitted(UpdateResult result) =>
        result.IsAcknowledged && (result.MatchedCount == 1 || result.UpsertedId is not null);

    static bool HasUndelivered(GrantState state) => state.Outbox.Any(entry => entry.State != OutboxState.Delivered);

    static BsonValue TokenIdValue(string? tokenId) => tokenId is null ? BsonNull.Value : new BsonString(tokenId);

    static string? StoredCurrentRefreshTokenId(BsonDocument document) =>
        document["currentRefreshTokenId"].IsBsonNull ? null : document["currentRefreshTokenId"].AsString;
}

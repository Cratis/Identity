// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Revocation;

/// <summary>Primary, majority-durable operational authority; never a projection or positive cache.</summary>
internal interface IFenceStore
{
    /// <summary>Reads validated authoritative state; absence is not permission.</summary>
    /// <param name="grantId">The authorization.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The state, or absence.</returns>
    Task<GrantState?> Read(string grantId, CancellationToken cancellationToken);

    /// <summary>Commits a permanent fence and all source-fact intents in one versioned write.</summary>
    /// <param name="grantId">The authorization.</param>
    /// <param name="subject">The account subject.</param>
    /// <param name="reason">The restriction.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The original fence on every retry.</returns>
    Task<GrantFence> Commit(string grantId, string subject, RevocationReason reason, CancellationToken cancellationToken);

    /// <summary>Admits the initial family and its outbox intent atomically; never overwrites an existing family.</summary>
    /// <param name="grantId">The authorization.</param>
    /// <param name="tokenId">The initial entry.</param>
    /// <param name="subject">The account.</param>
    /// <param name="startedAt">The admission time.</param>
    /// <param name="absoluteExpiresAt">The original absolute expiry.</param>
    /// <param name="link">The account link the grant was authenticated through, or null when unknown.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The authoritative winner, including a concurrent fence.</returns>
    Task<GrantState> RecordFamilyStart(string grantId, string tokenId, string subject, DateTimeOffset startedAt, DateTimeOffset absoluteExpiresAt, Guid? link, CancellationToken cancellationToken);

    /// <summary>Admits the initial family without an authenticating link; such a family is never admitted for refresh.</summary>
    /// <param name="grantId">The authorization.</param>
    /// <param name="tokenId">The initial entry.</param>
    /// <param name="subject">The account.</param>
    /// <param name="startedAt">The admission time.</param>
    /// <param name="absoluteExpiresAt">The original absolute expiry.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The authoritative winner, including a concurrent fence.</returns>
    Task<GrantState> RecordFamilyStart(string grantId, string tokenId, string subject, DateTimeOffset startedAt, DateTimeOffset absoluteExpiresAt, CancellationToken cancellationToken) =>
        RecordFamilyStart(grantId, tokenId, subject, startedAt, absoluteExpiresAt, null, cancellationToken);

    /// <summary>Consumes and installs against the same revision as revoke.</summary>
    /// <param name="grantId">The authorization.</param>
    /// <param name="consumedId">The presented entry.</param>
    /// <param name="successorId">The preallocated successor.</param>
    /// <param name="expectedVersion">The revision read by the caller.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The authoritative outcome.</returns>
    Task<RedeemOutcome> RedeemAndInstall(string grantId, string consumedId, string successorId, long expectedVersion, CancellationToken cancellationToken);

    /// <summary>Lists undelivered documents, including fences needing log recovery.</summary>
    /// <param name="limit">The batch bound.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The durable batch.</returns>
    Task<IReadOnlyList<GrantState>> Undelivered(int limit, CancellationToken cancellationToken);

    /// <summary>Confirms only the exact fence acknowledged by the log.</summary>
    /// <param name="fence">The confirmed restriction.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The durable confirmation.</returns>
    Task ConfirmLog(GrantFence fence, CancellationToken cancellationToken);

    /// <summary>Marks only an eligible, successfully appended intent delivered.</summary>
    /// <param name="grantId">The authorization.</param>
    /// <param name="outboxId">The intent.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The durable delivery update.</returns>
    Task Delivered(string grantId, string outboxId, CancellationToken cancellationToken);
}

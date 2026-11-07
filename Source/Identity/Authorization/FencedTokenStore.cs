// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Revocation;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using OpenIddict.MongoDb;
using OpenIddict.MongoDb.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Authorization;

/// <summary>Preserves OpenIddict storage, but binds every refresh insert to the single-document authority.</summary>
/// <remarks>7.8.0 exposes a settable BsonId ObjectId; base CreateAsync inserts it unchanged. Failed issuance
/// after the grant CAS can strand the family, but can never roll it back or admit two successors.</remarks>
/// <param name="context">The stock Mongo context.</param>
/// <param name="options">The stock options.</param>
/// <param name="fences">The authoritative grant store.</param>
/// <param name="issuance">The request-scoped preallocated successor.</param>
internal sealed class FencedTokenStore(IOpenIddictMongoDbContext context, IOptionsMonitor<OpenIddictMongoDbOptions> options, IFenceStore fences, RefreshIssuance issuance)
    : OpenIddictMongoDbTokenStore<OpenIddictMongoDbToken>(context, options)
{
    /// <inheritdoc/>
    public override async ValueTask CreateAsync(OpenIddictMongoDbToken token, CancellationToken cancellationToken)
    {
        if (token.Type != TokenTypeIdentifiers.RefreshToken)
        {
            await base.CreateAsync(token, cancellationToken);
            return;
        }

        var grantId = token.AuthorizationId.ToString();
        if (token.AuthorizationId == ObjectId.Empty || !AccountSubject.TryParse(token.Subject, out _) ||
            token.CreationDate is not { } created || token.ExpirationDate is not { } expires)
        {
            throw new RevocationPending();
        }

        GrantState state;
        if (issuance.SuccessorId is { } successor)
        {
            if (issuance.GrantId != grantId)
            {
                throw new RevocationPending();
            }

            token.Id = ObjectId.Parse(successor);

            // Retain the successful admission even if a later reuse/revoke has fenced the family.
            state = issuance.Admission ?? throw new RevocationPending();
        }
        else
        {
            // Allocate before recording the family: its initial admitted entry must be the actual inserted ID.
            token.Id = token.Id == ObjectId.Empty ? ObjectId.GenerateNewId() : token.Id;
            state = await fences.RecordFamilyStart(grantId, token.Id.ToString(), token.Subject!, new(created, TimeSpan.Zero), new(expires, TimeSpan.Zero), cancellationToken);
        }

        if (state.Fence is not null || state.CurrentRefreshTokenId != token.Id.ToString())
        {
            throw new RevocationPending();
        }

        var family = state.Outbox.SingleOrDefault(entry => entry.Kind == FactKind.FamilyStarted) ?? throw new RevocationPending();
        if (family.ExpiresAt is not { } absolute || absolute <= new DateTimeOffset(created, TimeSpan.Zero))
        {
            throw new RevocationPending();
        }

        token.ExpirationDate = expires <= absolute.UtcDateTime ? expires : absolute.UtcDateTime;
        await base.CreateAsync(token, cancellationToken);

        // A later revoke still wins future admission because the tombstone clears CurrentRefreshTokenId.
    }

    /// <summary>Allocates an entry ID without exposing Mongo types in request state.</summary>
    /// <returns>The representation accepted by the stock store.</returns>
    internal static string AllocateId() => ObjectId.GenerateNewId().ToString();
}

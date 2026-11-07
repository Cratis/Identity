<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Grant fences and refresh admission

A grant fence denies online refresh admission without waiting for Chronicle, a projection, a dispatcher, or OpenIddict cleanup. One majority-durable `grant-fences` document owns the revision, irreversible fence, current refresh entry identity, and source-fact delivery intents. Every replacement compares the document revision. Tombstones and delivery intents have no TTL.

Refresh allocates its successor identity before the grant CAS. That CAS consumes the presented identity and installs the successor identity against the same revision that revoke writes. If revoke commits first, refresh cannot install. If refresh commits first, revoke clears its installed successor and permanently denies subsequent admission. OpenIddict's later token-status update records reuse evidence, not the authoritative consume decision. Its failed redemption is explicitly rejected, unlike the stock handler's ignored failure. `FencedTokenStore` inserts the actual preallocated `ObjectId`; the pinned OpenIddict model exposes a settable `BsonId` and stock `CreateAsync` preserves it.

A non-current parent can still look valid in OpenIddict while the CAS winner updates its entry. Validation and the token endpoint let that known parent reach redemption, which rejects the loser and fences the family as reuse even before OpenIddict records redemption. Requests that read the same revision also detect the consumed parent after a lost CAS.

The winning request retains a scoped admission snapshot only after the CAS and successful OpenIddict redemption. A later fence does not reject its held response or successor insert: the request was ordered before that fence. The snapshot bounds the successor to the original family expiry and cannot authorize another request. The fence still denies that successor on next use, even when its insertion follows revocation cleanup.

A crash between the grant CAS and the successor insert can strand a family. Recovery never re-admits the consumed entry: the user must obtain a fresh grant. Initial creation and legacy-family repair atomically record the original family admission and expiry. Repair requires a live validated entry and authorization; missing, malformed, or unavailable authority/outbox state is not permission. During a rolling deployment an old-code replica can still issue through its old CAS; a successor not admitted by the new authority is denied and the user reauthenticates. Rolling back leaves unused grant documents, not new admission authority. Rotation does not slide the original absolute expiry or emit success telemetry facts.

Revocation atomically persists the tombstone and delivery intents first, runs the existing OpenIddict authorization/token cleanup, and confirms the exact immutable fence in the log before acknowledgement. A failure never acknowledges success or removes denial. Retries retain the fence identity, sequence, fact identities and reuse detection identity. RFC 7009 commits the fence before its built-in entry revocation; a retry must confirm a pending log before built-in `invalid_token` handling can become a success response.

## Durability boundary

**`CoLocatedMongoFenceLog` is an interim adapter and does NOT meet B1.** It shares the operational Mongo database and its restore boundary. It verifies identical content on repeated fence identity, rejects conflicts, and preserves the original sequence. Independently durable log storage, recovery epochs, and the restore gate remain #42; this implementation does not make an older backup safe to serve.

The outbox worker recovers pending log confirmation before appending any restriction fact. It uses Chronicle's durable `AppendOnce` key `(FactId, EventTypeId)` and marks delivery afterward. Replaying after append but before that mark uses the same key. Unsupported keyed appends, conflicts, and errors hold delivery; there is no unkeyed fallback. Chronicle 19.111.0 exposes these APIs; its `AppendResult.IsSuccess` does not include idempotency conflicts, so the writer checks that flag separately. Deployments require a kernel advertising `SupportsIdempotentAppends`.

## Access-token and consent boundaries

Offline access JWT validation retains the accepted **630-second residual window** after revocation: a 600-second token lifetime plus 30 seconds of validation skew. A grant fence does not revoke a JWT already accepted by offline validation. Online refresh checks the primary grant authority on every attempt. The bounded access-token window is a contract, not a promise of immediate offline invalidation.

`GrantBoundRevocationFence` is conservative infrastructure for the dormant consent flow, not a new endpoint. A clear grant alone cannot prove session liveness or restore safety. Without authoritative session/recovery evidence it denies activation. It is not wired over the existing consent defaults; live consent integration remains separate.

## Internal event identities

These generation-one IDs follow the repository's explicit UUID `[EventType]` convention. Operational entry identities and submitted token values never enter their schemas. Grant/family event-source UUIDs use a domain-separated deterministic mapping of the operational authorization identifier. There is no Public reactor or publication until the revocation gate permits it.

| Internal source fact | Event type ID |
| --- | --- |
| `AuthorizationRevocationRecorded` | `ac88f9a7-6364-4f20-9301-d533b491b641` |
| `RefreshTokenFamilyRevocationRecorded` | `a80e52ab-051e-4771-bff2-488dccd8aa44` |
| `RefreshTokenFamilyStarted` | `d9a448be-ccf1-4ab5-b43c-f17dad0ba6c3` |
| `RefreshTokenReuseDetected` | `bedbc6d5-2870-4cf7-aa5e-79e7a2bf3cb6` |

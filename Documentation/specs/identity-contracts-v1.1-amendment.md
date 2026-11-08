# Identity contracts v1.1 amendment: events, tenant model, delegation and revocation

```text
Spec id: identity-contracts-amendment
Version: 1.1
Status: Draft
Issue: https://github.com/Cratis/Identity/issues/104
Sign-off issue: https://github.com/Cratis/Identity/issues/14
Supersedes: None
```

## 1. Status and scope

This is a **Draft** proposal. It is not accepted and it changes nothing by itself. It batches every contract change found since the v1 lock into one review, to be read together with [Identity #99](https://github.com/Cratis/Identity/pull/99). The Locked v1 specifications stay authoritative until the owners decide each item in [Identity #14](https://github.com/Cratis/Identity/issues/14). An accepted item then lands as a new version file of each specification it alters, as the [conventions](README.md#conventions) require; the v1 files and their vectors stay unchanged.

RFC 2119 keywords below describe the proposed contract, not deployed behavior. For each item: the proposed change, why (the evidence), and the Locked sections it alters. Unresolved points are open questions in section 7, not decisions.

| Item | Source | Alters |
|---|---|---|
| 2.1 C1 federation through a realm | #104 | identity-tenant-model-v1 §6, §7 |
| 2.2 C2 grant ownership | #104 | arc-delegation-v1 "Identity and grant record", "Attenuation and revocation", "B5/C4", sign-off decisions |
| 2.3 C3 consent activation barrier | #104, #100 | identity-events-v1 §4.1, §8 `ConsentGranted`, `AuthorizationGranted` |
| 2.4 C4 approval vs external execution | #104 | arc-delegation-v1 "Authorization and approvals" |
| 3 Client and resource event fields | #107 | identity-events-v1 §8 Clients and Resources rows, §6 |
| 4 `FactId` on restriction facts | #110 | identity-events-v1 §4.1, §8 `UpstreamKeyLinkQuarantined`, `UpstreamKeyLinkCleared` |
| 5 Atomic writer cancellation for account appends | #110 | identity-revocation-v1 §5, §6; identity-events-v1 §8 (new row) |
| 6 Activation abandonment reason | #99, #100 | identity-events-v1 §8 `ConsentWithdrawn`, `AuthorizationRevoked`; identity-revocation-v1 §7 |

## 2. Design corrections C1–C4 (Identity #104)

The corrections are written up in the design note `Documentation/design/identity-platform-corrections-v1.md` (on the #99 branch, not on main). That note edits no specification and lists its conflicts in its section 6. This section turns each conflict into proposed specification text.

### 2.1 C1: federation through a Cratis realm by default

**Change.** Add to identity-tenant-model §6:

> A customer identity provider (OIDC first, SAML later) MUST be connected by default as an upstream of a Cratis Identity realm. The realm issues the tenant- and resource-bound credential (`tid`, `aud`) only after tenant selection and a live membership check. A token issued by an external authorization server MAY be accepted directly only when the token itself carries an immutable tenant and resource constraint that the installation verifies through configured, issuer-scoped claim mapping (for example a per-tenant audience or app registration). Otherwise AuthProxy MUST exchange the external token at tenant selection for a realm-issued, tenant-bound session, and bearer routes MUST refuse the raw external token.

Add to §7 step 1: "A raw external token that does not meet the direct-mode condition in §6 MUST be refused at this step." Steps 2 and 3 already say caller input selects the target only; no change.

**Why.** A person who is a member of tenants A and B could replay one external token against both. A membership check does not bind the credential to one tenant, so §7 step 3's exact comparison has no credential-side tenant to compare with.

**Alters.** identity-tenant-model-v1 §6 (Credential constraints), §7 step 1.

### 2.2 C2: issuer grants and task authority have separate writers

**Change.** In arc-delegation:

- Identity is the only writer of **issuer grants**: OAuth consent and authorization records, refresh families, revocation fences and, later, RFC 8693 exchange lineage for tokens it issues.
- The product is the only writer of **task authority**: a task delegation for its own agent (actor, represented person, task, product-qualified scopes, resource, environment, usage and time bounds), prepared operations, approvals, execution records and usage counters, in its own store.
- A task delegation MUST name its parent credential (session, PAT or OAuth grant id), taken from the verified caller. At creation and on every use, the product MUST check the parent's revocation status within the revocation bound of identity-revocation-v1.
- v1.1 is **one hop**: a person to that product's agent, same resource, no token exchange. The depth cap becomes one edge; depth 2 and exchange MUST be refused.
- Budgets are product-local and MUST be counted atomically in one store. The parent-debit transaction, nested budgets and cross-product exchange are deferred. `rootGrantId`, `parentGrantId`, `authorizedExchangeActors` and depths above 1 are reserved: they MUST NOT be reused with a different meaning.
- Unchanged: grant lookup only from the verified credential, exact equality of `accountKey`, actor, resource, tenant and environment, `policyEpoch`, `recoveryEpoch`, the idempotency and recovery-epoch replay rules of "B5/C4", and attenuation (narrowing only).

**Why.** v1 assigns the same grant record to two writers (Identity and the product), and RFC 8693 offers no atomic budget across products, so the cross-product parent-debit transaction cannot be built as specified.

**Alters.** arc-delegation-v1 "Identity and grant record" (record fields), "Attenuation and revocation" (depth cap, exchange), "B5/C4: budget atomicity and retries" (parent debit), "Decisions recorded at sign-off" (two-edge cap). The design document's "Who writes which fact" row says "grants" without the split; it is a design document and changes with #99.

### 2.3 C3: consent and grant creation commit through an activation barrier

**Change.** The barrier itself (Pending → Decided → EvidenceRecorded → Active, each a compare-and-set in the operational store, no usable grant or authorization code before durable evidence) stays in the design and module outline (S4). The contract change is the evidence identity in identity-events-v1. Two options:

- **A (recommended, keeps the Locked placements).** `ConsentGranted` stays on Account and `AuthorizationGrantRecorded` on Grant. Both facts take a `FactId` derived deterministically from the server-generated `ConsentRequestId` (one derivation per fact kind), used as the Chronicle idempotency key. §4.1 point 3 adds: "For grant creation, `FactId` is derived from the consent request." Evidence counts as recorded only when both facts are confirmed.
- **B (as written in C3).** Both facts share one event source derived from `ConsentRequestId`. This changes the §8 event-source column for both rows and breaks §4.1's rule that a source fact sits on its Public contract's event source.

Also: a deny MUST append nothing usable, and evidence resolution MUST distinguish recorded, not recorded (permanently sealed against later appends) and unknown, as implemented dormant in #100.

**Why.** The design note's §6 flags that C3's shared stream contradicts §8 and §4.1. Identity #100 implemented the state machine behind a fake evidence port and stopped because the event identities and complete-append atomicity are undefined.

**Alters.** identity-events-v1 §4.1 point 3; §8 rows `ConsentGranted` and `AuthorizationGranted` (and, for option B, the source-fact table entry `AuthorizationGrantRecorded`).

### 2.4 C4: approved intent is separate from external execution

**Change.** Add to arc-delegation "Authorization and approvals":

> Consuming an approval and creating an execution record (execution id, the canonical prepared operation, the destination idempotency key or precondition, state `Dispatching`) MUST commit atomically in the product's store. "Exactly once" applies to that local commit only. The dispatcher MUST perform the external effect through the destination's idempotency key or a conditional operation and record exactly one outcome: `Succeeded`; `Failed` (definitive, the person may prepare the operation again); or `OutcomeUnknown` (timeout, crash after send, ambiguous response). `OutcomeUnknown` MUST be reconciled by reading the destination's state. A write MUST NOT be retried blindly. If reconciliation cannot decide, the state stays `OutcomeUnknown`, is shown to the person, and a retry needs a new approval.

The product mapping (for example GitHub push with force-with-lease, merge with `sha`) stays product documentation, not contract.

**Why.** v1 requires single consumption of an approval but says nothing about the execution record or an unknown outcome. A local transaction cannot make a push or merge happen exactly once.

**Alters.** arc-delegation-v1 "Authorization and approvals". identity-revocation-v1 §7's Approval row is consistent and does not change.

## 3. Client and resource event fields (Identity #107)

**Change.** All new fields are explicit; authorization is never inferred from `RegistrationKind`, and a client with no recorded permission has none.

| Row | Proposed fields (added in bold) |
|---|---|
| `ClientRegistered` (Internal) | ClientId, RegistrationKind, RegisteredAt, **ClientType** (Public, Confidential), **ApplicationType** (Native, Web), **DisplayName**, **RequirePkce**, **ConsentType** (Explicit, none), **Endpoints**, **GrantTypes**, **ResponseTypes** (closed enumerated sets), **ResourceIndicators**, **Scopes** (explicit normalized sets, never "the whole catalog") |
| `ClientSecretRotated` | unchanged; the initial introspection-client secret enters only as a keyed `SecretDigest` |
| `ClientRedirectUrisChanged` | unchanged; note: loopback IP literals `127.0.0.1` and `[::1]` are valid, `localhost` is refused |
| `ClientRetired`, `ResourceScopeRetired` | unchanged |
| `ResourceRegistered` (Public) | ResourceIndicator (normalized), Audience, ScopePrefix, **DisplayName** |
| `ResourceScopeRegistered` (Public) | ScopeName, **Description** (product text; no personal data or secret; §6 check applies) |

The enumerated sets are closed values in the contracts project, not free strings or OpenIddict permission strings. No field carries a secret or personal data. Shape: generation 1 if no store holds these events (the repository has no CLR type for any of them, and §8 still says "assigned at implementation"); otherwise generation 2, whose upcast yields an empty permission set (fail closed).

**Why.** The configuration-backed client and resource stores (#37) build today's clients from code (`ClientDescriptors`: client type, PKCE, endpoint, grant, response, scope and resource permissions, display name) and the consent page reads resource display names and scope descriptions. `ClientRegistered` carries only ClientId, RegistrationKind and RegisteredAt, so an event-backed store cannot reproduce them.

**Alters.** identity-events-v1 §8 rows `ClientRegistered`, `ClientRedirectUrisChanged` (note only), `ResourceRegistered`, `ResourceScopeRegistered`; §6 safe-concept registry (the enumerations).

## 4. `FactId` on restriction facts (Identity #110)

**Finding.** The audit reads §4.1 point 3 (line 80, "Every source fact carries a `FactId`") as contradicting the `UpstreamKeyLinkQuarantined` row (line 187, no `FactId`). Read literally they do not conflict: §4.1 applies to the **Internal source facts of Public contracts** (its table, lines 87–100), and `UpstreamKeyLinkQuarantined` is a standalone Internal fact with no Public contract. The real gap is elsewhere: identity-revocation-v1 §6 step 2 requires every restriction fact, including link removal, to be re-established after a restore, and §6 step 3 requires re-appends with the original idempotency keys. A quarantine hold is a restriction (losing it lets a link confirm before `HoldNotBefore`), but the event has no stable identity to replay under. The code agrees with the row: the CLR type (`a407f6de-…`, generation 1) has no `FactId`, while the account source facts do.

**Change.**

- Add §4.2 to identity-events: "Every Internal fact that establishes or lifts a restriction that identity-revocation-v1 §6 must re-establish carries `FactId` with the semantics of §4.1 point 3, whether or not it has a Public contract."
- `UpstreamKeyLinkQuarantined`: add `FactId`. Its type id is pinned and the type is appended today, so under §3 this is generation 2. The upcast derives `FactId` deterministically from the event's `EvidenceId` (unique per quarantine in the current writer); it invents no authority. Generation 1 may be corrected in place only if the owners confirm that no store holds the event.
- `UpstreamKeyLinkCleared` (the lift of a hold): add `FactId` the same way, if lifting a hold is in recovery scope (open question).

**Alters.** identity-events-v1 §4.1 (new §4.2), §8 rows `UpstreamKeyLinkQuarantined` and `UpstreamKeyLinkCleared`.

## 5. Atomic writer cancellation for account appends (Identity #110)

**Finding.** Slice 1 (grants, branch `identity-110-accounts-links`) makes a reserved writer cancellable atomically. A reservation in the independent fence log moves to `Fencing`; `MongoFenceStore.Cancel` adds the reservation id to `cancelledReservations` on the **same operational grant document** the writer commits to, and the writer's conditional commit filters on `cancelledReservations $ne <id>`. Either the writer committed (the record is recovered) or the cancellation is durable and the reservation may be voided. After a restore, `ReplayVoids` re-applies every cancellation before readiness. Slice 2 (account status and links, `.ai-work` plan only, not implemented) commits to **Chronicle**: `AccountStore.TryAppend` is a guarded `AppendMany` with the account decision token and uniqueness constraints. There is no shared document to hold a cancellation marker, so a writer delayed after its ledger reservation could still append after its reservation was voided. identity-revocation-v1 §5 says pending commits "MUST be reconciled, not erased", but defines no cancellation for a writer whose commit point is the event log.

**Change.**

- identity-revocation §5, new paragraph: "A reservation MAY be voided only after a durable cancellation that the reserved writer's commit cannot pass, recorded at the writer's own commit point. Where the commit point is the Chronicle event log, the cancellation is an Internal fact on the writer's event source, appended under the same concurrency guard the writer uses, and the writer's decision MUST refuse a cancelled `FactId`. After any restore, cancellations of voided reservations MUST be re-appended before readiness."
- identity-events §8, new Internal row: `AccountFactWriteCancelled` | Accounts | Internal | Account | CancelledFactId, CancelledAt | Identity | "Cancels one reserved restriction write; never a business fact; idempotent per `CancelledFactId`."

With the guard, cancel and write serialize on the account stream: if the write lands first, the canceller's fold sees the `FactId` and recovers it; if the cancellation lands first, the writer's guarded append conflicts and its re-read refuses.

Alternative: the canceller claims the writer's Chronicle idempotency key (`FactId`) with the cancellation fact, so the writer's keyed append conflicts. It needs no decision-fold change, but whether Chronicle idempotency keys can be combined with the decision guard and the uniqueness constraints in one append is unverified (the slice 2 plan names it as a stop condition).

**Alters.** identity-revocation-v1 §5 (Invariant B1), §6 step 2; identity-events-v1 §8 (new row) and §4.2 from item 4.

## 6. Activation abandonment reason (Identity #99, #100)

**Finding.** C3 says that when activation can no longer happen, the reconciler appends the existing withdrawal fact with a system cause and the reason `ActivationAbandoned`, and asks whether the Locked catalogue allows that. `ConsentWithdrawn` carries `ConsentId, ReasonId, WithdrawnAt` and describes a person's withdrawal. Identity #100 (dormant, branch `identity-100-grant-activation`) narrowed the case. When evidence is confirmed absent and sealed, the request ends `Abandoned` and no fact is needed. When evidence is recorded but activation is forbidden (expiry, ended session, fence), the request ends `EvidenceWithheld` with **no** Chronicle fact; the published `AuthorizationGranted` then describes a grant that was never usable, and the 30-day terminal row retention may delete the only operational record before a closing fact exists.

**Change.** Option A is recommended:

- **A.** Allow a system cause. `ConsentWithdrawn` notes: "Written for a person's withdrawal or, with `ReasonId` `activation_abandoned`, by the consent reconciler for a request whose evidence was recorded but which never became active." The matching grant closes through `AuthorizationRevoked` with the same reason, under the normal fence, so consumers see one closing contract. identity-revocation-v1 §7 (consent withdrawal fences all affected grants) applies unchanged.
- **B.** A new Internal fact `AuthorizationGrantAbandoned` (Grant, `FactId`, `ReasonId`, `AbandonedAt`) with a Public contract, leaving `ConsentWithdrawn` person-only.

For both: the closing facts are written only from `EvidenceWithheld`, never when evidence is absent, with a `FactId` derived from the consent request (item 2.3), and an `EvidenceWithheld` row MUST NOT be TTL-deleted until they are confirmed. Reason ids are snake case, like the other reason ids in identity-events (`activation_abandoned`, not `ActivationAbandoned`).

**Alters.** identity-events-v1 §8 rows `ConsentWithdrawn` and `AuthorizationRevoked` (notes; option B adds rows and a source-fact table entry); identity-revocation-v1 §7 (consent withdrawal paragraph) for the system cause.

## 7. Open questions

1. **File naming.** The conventions name versions `-v<major>`. Do accepted items land as new major files (`identity-events-v2.md`, `arc-delegation-v2.md`, …) or is a v1.1 minor version allowed? This Draft is not added to the index until that is decided.
2. **C2.** With one hop and no exchange, does v1.1 still issue delegated tokens with `act`, or does the agent act only under product-local task authority? Which grant-record fields stay required?
3. **C3.** Is a Chronicle `AppendMany` across two event sources (Account and Grant) atomic? If not, option A needs per-fact confirmation, or option B.
4. **#107.** One wide `ClientRegistered`, or a separate permissions event so permissions can change without re-registration? May Public resource events carry `DisplayName` and `Description`, or must consent text stay Internal? Is `ResourceIndicator` one value or a normalized set? Can anyone with production access confirm that no store holds client or resource events, so generation 1 is acceptable?
5. **Item 4.** Does any store hold `UpstreamKeyLinkQuarantined` generation 1 (deciding generation 2 versus an in-place correction)? Is lifting a hold (`UpstreamKeyLinkCleared`) in recovery scope?
6. **Item 5.** Guarded cancellation fact or idempotency-key claim? Needs a real-kernel check of keyed appends with the decision guard and constraints. Should foreign writers appending account facts directly to Chronicle be forbidden by contract, since custody cannot cover them?
7. **Item 6.** Option A or B, and should `AuthorizationGranted` publication be visible to connected apps before activation at all?
8. **Lock order.** The audit recommends not locking v1.1 until #110 slice 2 and #107 show it is sufficient. Is that the review plan?
9. **`name` claim.** See section 8: confirm the deviation or restore the v1 prohibition.

## 8. Display name `name` in access tokens (idp-claims delivery)

**Finding.** identity-contract-v1 §5 (around line 148) says display name MUST NOT appear in access tokens; only `preferred_username` is excepted, and only for resources that opt in. The idp-claims delivery (branch `identity-idp-claims`, head 4c44404) restores `name` in access tokens for **all** audiences, on the instruction of L0 (keep-name). Other upstream identity claims (`preferred_username`, `github_id`, `github_login`, `idp_sub`) stay limited to audiences that receive upstream identity.

**Change.** Option A is what the code does today:

- **A.** Amend identity-contract-v1 §5 to allow `name` in access tokens for every audience, as display-only data that carries no authority. The code carries a comment recording the deviation.
- **B.** Keep the v1 prohibition and drop `name` again, leaving it to the user-info or profile surface.

**Alters.** identity-contract-v1 §5 (the sentence listing display name among the claims that MUST NOT appear in access tokens).

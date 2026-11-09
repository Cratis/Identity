# Identity design: binding excerpts

```text
Doc id: identity-design
Version: 1
Status: Signed off
Signed off: 2026-11-05 by woksin, https://github.com/Cratis/Identity/issues/14; owner confirmation pending
Source: W0 identity design (final), signed off by a maintainer 2026-11-05 (#14), owner confirmation pending
Issue: https://github.com/Cratis/Identity/issues/11
Cited by: identity-module-outline v1
```

## Scope and authority

This note records the parts of the W0 identity design (final), signed off by a maintainer 2026-11-05 (#14), owner confirmation pending, that the [Identity module outline](identity-module-outline-v1.md) relies on. The excerpts below are **verbatim** where the text is binding. Section numbers (§…), decision ids (D…) and cross-references inside an excerpt refer to the W0 identity design, which is the only place they are defined; sections not excerpted here are not part of this note and are not cited by the outline.

Where this note and a signed-off specification differ, the specification wins: [identity-contract](../specs/identity-contract-v1.md), [identity-events](../specs/identity-events-v1.md), [identity-revocation](../specs/identity-revocation-v1.md) and [identity-package-and-storage](identity-package-and-storage-v1.md).

Each excerpt has a stable heading; the outline links to those headings. Do not rename them.

## Accounts and linked keys

*W0 identity design §4.3, "Accounts, people and linked keys".*

- **Account key.** Everywhere outside Identity, an account is keyed by the exact pair `(issuer, subject)`: the realm's issuer URL and the opaque account id it issues. A subject alone is never a key, because two realms can issue the same subject.
- **Account** (Identity): that opaque id, plus linked upstream identities, each stored as an exact `(upstream issuer, upstream subject)` pair. Never email, never a display or login name.
- **Person** (product): the product's own record, holding a set of linked keys: the realm `(issuer, subject)`, and while migrating, the legacy GitHub **numeric user id** and AuthProxy's canonical `(provider key, authority, subject)` tuple. GitHub login names may be shown, but they change on rename and are never proof for linking.
- **Linking rules.**
  - Each linked key belongs to at most one person per product (uniqueness enforced by the product's single writer).
  - Linking is an explicit, authenticated act: the person shows intent ("link this account") and proves control of **both** sides in the same flow, for example a fresh login with each.
  - Automatic migration links only when the immutable upstream id matches exactly (for example the GitHub numeric id carried by Identity's `github_id` claim matches the stored legacy id).
  - **Never link by email match.** Ambiguous or conflicting legacy records go to a quarantine list for a human to resolve; quarantined records grant nothing.
- Identity emits generic claims for the upstream (`idp`, `idp_sub`), keeping today's `github_id` claim during migration (Design A).

## Who writes which fact

*W0 identity design §4.9, "Who writes which fact".*

One authoritative writer per fact; everyone else asks or reacts.

| Fact | Authoritative writer | Notes |
|---|---|---|
| Account, linked upstream identities, grants, consent | Identity | Linking is immutable once confirmed; changes are new facts |
| Organisation identity, organisation policy, tenant bindings | Access (logical module, hosted in Identity for now) | Bindings need confirmation from both sides (§5.4) |
| Workspace (tenant) membership and roles | The product | Checked live on every request |
| Invitation issued, accepted; organisation requested | Ante | Acceptance is neither membership nor readiness (§5.3) |
| Provisioning readiness | The product, reconciled against Ante's facts | Durable, retried reconciliation |
| Delegations, grants for agents, approvals | The product (Arc building blocks) | §6.5 |
| Runner registration, workload credentials | The product or the platform (machine plane) | Never the people issuer |

## Identity as a headless event-sourced module

*W0 identity design §4.10, opening.*

Owner input 4 (§0) changes what Identity *is*: not a standalone server with its own pages and database, but a NuGet module, built on OpenIddict, whose application-facing surface is Arc commands, queries and Chronicle events, and whose UI is built by the application that hosts it.

## Module packages

*W0 identity design §4.10, "Packages", the Usher shape.*

| Package | Contents |
|---|---|
| `Cratis.Identity` | The module: Arc commands and queries, Chronicle events, read models and reactors, the OpenIddict server and Chronicle-backed stores, upstream federation, Access (D16). Host calls `builder.AddCratisIdentity(...)` and `app.MapCratisIdentity()`. |
| `Cratis.Identity.Contracts` | Public events products react to (account registered, upstream key linked, authorization revoked, account erased …). |
| `@cratis/identity` | Generated TypeScript proxies for the commands and queries. |
| `@cratis/identity.react` | Optional reusable components on `@cratis/components`: sign-in chooser, consent, device-code entry, linked accounts, connected apps. A host may use them, restyle them, or write its own. |

## Module surface

*W0 identity design §4.10, "The surface: commands, queries and events, plus the protocol".*

Everything an application does with Identity is a command (register client, approve consent, link upstream key, revoke authorization, erase account …), a query (my connected apps, pending consent request, clients, accounts) or an event. The OAuth and OpenID Connect endpoints (authorize, token, revoke, introspect, discovery, JWKS) are not an application surface: their shape is fixed by the standards, and `MapCratisIdentity()` maps them inside the module. They are thin, and every state change they make goes through the same commands. This is the one place where "only commands, queries and events" cannot hold literally.

## What is event sourced

*W0 identity design §4.10, "What is event sourced, and what is not".*

| State | Event sourced? | Why |
|---|---|---|
| Accounts, linked upstream keys `(issuer, subject)`, account disabled/erased | Yes | Long-lived facts with audit value. One account per upstream key is enforced with a Chronicle unique constraint. |
| Clients (pre-registered, CIMD-observed), redirect URIs, secret rotations | Yes | Hashes of high-entropy secrets only, as Chronicle does; read model feeds the OpenIddict application store. |
| Resources, scopes, Access facts (organisations, policies, tenant bindings) | Yes | Configuration-like facts with one writer (§4.9). |
| Authorizations (consent grants) and their revocation; refresh-token family started, revoked, reuse detected | Yes | User-meaningful, low volume, needed for "connected apps" and audit. |
| Individual refresh-token rotations, authorization codes, device and user codes, `state`/nonce, replay caches (`jti`), CIMD fetch cache, rate-limit counters | **No**: an operational store with TTL (OpenIddict token store) | Short-lived, single-use, high-volume, needing an atomic compare-and-set on redemption; no business meaning; storing redeemable material in an append-only log would make it permanent. |
| Revocation and disable fences (tombstones), grant and account epochs, the outbox of pending revocation events | **No**: the operational store, durable and replicated, retained (below) | Must be authoritative before the command returns, independent of Chronicle's availability. |
| Access tokens | **Not stored** | Short-lived signed JWTs; revocation acts on the grant (below). |
| Signing, encryption and Data Protection keys | **Never in Chronicle** | Secrets. Keys come from the host's secret store or KMS; the Data Protection key ring stays in a protected store, not the event log. |
| Failed sign-in attempts | No (logs and metrics) | Attacker-driven volume and personal data; an event per attempt would turn the log into a flooding target. |

## Revocation protocol

*W0 identity design §4.10, "Security, honestly", revocation protocol (B1).*

Read models are eventually consistent, so revocation never waits for a projection. One protocol covers revoking a grant or token family and disabling an account:

1. **Fence and outbox, atomically.** The command commits, in one operational-store transaction, a fence (a tombstone for the grant, family or account, and the incremented grant or account epoch) and an outbox entry for the domain event. The fence is also appended, before the command returns, to the **fence log**: an independently durable record of acknowledged fences (for example a replicated append-only log, or a write-ahead log in the secret and operations store), kept outside the backup sets of both the operational store and Chronicle, with its own recovery watermark. The operational commit and the fence-log append together are the acknowledgement; the command returns after both, whether or not Chronicle is up.
2. **Serialised redemption.** Refresh, token exchange, introspection and code redemption read the fence in the same store and change a family only by compare-and-set against it, so a refresh either commits before the fence (and is the last) or fails. A family or grant the store does not know is refused. This store answers *credential status* only; current Access policy is still checked through the policy epoch (§4.9).
3. **Asynchronous, idempotent append.** A dispatcher appends the event from the outbox (event id = outbox id) and retries while Chronicle is down; the entry is removed only after the append is acknowledged. Outbox age is alerted.
4. **Tombstone retention.** Tombstones, and fence-log entries, outlive everything they revoke (refresh absolute lifetime, longest grant) and the longest backup retention of either store.
5. **Recovery.** Each store keeps a watermark: the operational store its highest fence sequence, the event store the highest fence sequence it has appended. After any restore Identity starts in recovery mode and refuses refresh, exchange and new grants (fail closed) until revocation events newer than the operational watermark are re-applied as fences and fences newer than the event-store watermark are re-enqueued. Fence-log entries newer than the restored operational watermark are re-applied as fences and their events re-enqueued. The event-store watermark alone never proves completeness: an operational backup can satisfy it and still predate a fence whose event never reached Chronicle. If completeness of fences since the backup cannot be proven from the fence log (unavailable, or its retained range does not reach back to the restored watermark), Identity bumps the realm or grant policy epoch, invalidating every credential that could be affected, before it serves anything.
6. **Tests.** A crash between every step (after the fence, after the append, before the outbox entry is removed); refresh racing revoke; projection rebuild with projections paused; restore of either store older than the other; a duplicate append; a crash after acknowledgement and before the event append, then both stores restored from backups older than the fence (the credential stays revoked).

A revoked refresh token fails on its next use; an access token already issued lives until it expires (the §7.2 targets), unless AuthProxy introspects.

### Replay and rebuild boundary

Rebuilding business read models is not an issuance path. The current projections and reducer have no dependencies; the sole reactor publishes account contracts through the event store using the stable `FactId:contractTypeId` identity. An observer-inventory spec fails when a new observer has not been classified and checks declared dependencies for mail, token issuance or grant mutation. This is a structural tripwire, not behavioral proof that replay cannot produce those effects.

Unit replay specs rebuild all four models twice, keep operational revocation fences authoritative and scan serialized source-log/outbox payloads and envelopes for synthetic secrets. Consent activation is exercised with in-memory doubles: replay cannot activate abandoned or withheld requests, make never-active evidence usable, bypass revocation or repeat a code claim. Real-kernel consent replay remains with [#99](https://github.com/Cratis/Identity/issues/99); this coverage is not a claim of full restore conformance. See [event replay checks](../specs/identity-events-v1.md#611-replay-regression-checks) and [revocation rebuild coverage](../specs/identity-revocation-v1.md#91-unit-replay-and-rebuild-coverage).

## UI moves out, the security does not

*W0 identity design §4.10, "Security, honestly", UI rule.*

Interaction pages (sign-in, consent, device code) must be served on the issuer's own origin. The package keeps the decision: a host UI can only approve a pending, single-use consent request bound to the browser session; the package enforces anti-forgery, `frame-ancestors 'none'`, escaping of client-supplied values, and the "only continue if you started this" warning. A conformance spec in the package runs against any host UI; Stage-rendered UIs pass it by construction.

## Realm consequences

*W0 identity design §4.10, "Realm consequences", first and last bullets.*

- *A realm is a host.* Each realm is one host application embedding `Cratis.Identity`, with its own issuer URL, keys and **its own Chronicle event store**; the event store is the realm's data boundary. D1 is unchanged; "the same image with different configuration" becomes "the same package in different hosts".
- *Signing-key custody per host.* Every host holds only its own realm's keys, from its own secret store or KMS; the package refuses to start without them outside development; rotation is drilled per host (§8.4). A host serving several realms (D19) keeps each realm's keys and event store separate.

## Hard-to-change contracts

*W0 identity design §4.11, rows "Person id" and "Identity events". The table is the contract the outline must not change.*

| Contract | Decision | Standard | Why it is hard to change | Who decides |
|---|---|---|---|---|
| Person id | A person is the realm's `(issuer, subject)`; the subject is an opaque, pseudonymous, never-reassigned account id, never an email or upstream login. Upstream identities are linked `(issuer, subject)` keys on immutable upstream ids (§4.3) | OIDC Core: `sub` unique and never reassigned within an issuer (UNVERIFIED wording) | It is in every event, membership, grant, PAT and audit record | D4 (decided as recommended); which issuer is D1 |
| Identity events | Past-tense facts in `Cratis.Identity.Contracts`, each with a stable event type id; split into public (consumed by other modules) and internal events. Changed only by a new generation with upcast and downcast migrations; never renamed, removed or reused. `[PII]` on personal fields from the first generation; no secret, code or token value in any event (§4.10). Ante and Access follow the same rules | Chronicle event generations | Events are kept forever in every realm's store; consumers read them for years | einari and the Identity owner (D18) |

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Operational consent activation

This is dormant infrastructure, not a replacement for the current authorization endpoint. It writes no Chronicle events and issues no OpenIddict authorization codes. The live consent flow is unchanged. Its namespace is `Cratis.Identity.ConsentFlow` to avoid colliding with the existing `Authorization.Consent` model.

`ConsentActivation.Create` accepts only already validated server-side authorization parameters. The caller must allocate a distinct `AuthorizationRequestId` for the original authorization request and carry it in server-side or protected state together with the resulting consent request identity. The binding stores this opaque, non-sensitive correlation identity, not raw authorization parameters. Live request creation is deferred to the post-#99 wiring issue; #78 adds no live creator. Before that wiring, the endpoint must validate the protected proof and the authenticated session and reconstruct the exact immutable binding from that protected request. Never use submitted client, realm, account, tenant, resource, scope, session or authorization-request identities as that binding.

Approval compare-and-sets `Pending -> Decided`; denial closes the request without evidence. `Resume` idempotently records evidence, persists `EvidenceRecorded`, prepares a dormant authorization, rechecks the deadline and live fence, and compare-and-sets `Active`. Only `ClaimCode` can consume the single permission to issue a code. It requires the exact binding and a confirmed live fence. A crash after consuming that permission intentionally requires a fresh consent request, rather than issuing a second code. Actual code redemption and its single-use enforcement remain OpenIddict's responsibility.

## Pending query and decisions

`PendingConsent.ForCurrentSession` is an authorized, parameterless model-bound query. It resolves the account from the authenticated principal, the session through `ICurrentConsentSession`, and the realm from the configured issuer. The default session resolver returns no session: the query returns nothing and both commands refuse uniformly. The real session claim and session fencing belong to #99/S6; sign-in is unchanged. Specs create requests through `ConsentActivation.Create` and use fake sessions.

The query finds the newest unexpired `Pending` request by immutable server-clock `CreatedAt` and rechecks account activity, client authorization/scope permissions, resource scope policy and selectable tenants. It returns HTML-escaped display text, tenant identifiers, expiry, the warning "Only continue if you started this.", and a protected proof. It returns no authorization-request identity, PKCE, redirect URI, session identity, logos or navigation URIs. Escaped text is presentation data, not host-provided HTML.

`ApproveConsent` accepts a request identity, selected tenant, selected scopes and proof. `DenyConsent` accepts only the request identity and proof. Neither accepts caller, realm, client, resource or continuation bindings. `IConsentProof` uses purpose-separated data protection bound to request identity, account, session, realm and deadline. Missing, altered, cross-binding and expired proofs fail closed. The atomic `Pending` transition makes a valid proof single-use; there is no mutable token table.

Approval revalidates current eligibility and requires exactly the requested scope set (v1 does not support narrowing), with no duplicates or unknown scopes. It stores a canonical `ConsentSelection` in `Pending -> Decided`. The selection is immutable once set and survives recovery to `Abandoned`; abandonment directly from `Pending` has no selection. Denial stores no selection and transitions `Pending -> Denied` with terminal retention. Every validation or losing-CAS refusal is `consent_decision_refused`, without disclosing request existence. Neither command calls the evidence writer, activator, `Resume` or code issuance. Catalogue reads are passive eligibility checks, not the #99 revocation/session fence.

The real evidence and activation adapters must consume the recorded selection, not infer a new choice from mutable catalogues or submitted bindings. #48 consent conformance remains a follow-up until live request creation and the #99/S6 protocol wiring are available.

## Deferred adapters

The default `IConsentEvidence` always returns `Unknown`, the default `IRevocationFence` denies, and the default `IAuthorizationActivator` returns no authorization. Thus the new path cannot reach `Active` with the shipped defaults, even if it is called directly. Applications may supply the eventual adapters before registration; registrations use `TryAdd`.

A real evidence adapter is blocked on #99. `Record` must deduplicate the complete evidence append by `ConsentRequestId`. `Resolve` must serialize against **both in-flight and future appends**. `NotRecorded` means no evidence exists and the identity has been permanently sealed against append. A partial append, unavailable writer or lost acknowledgement with no authoritative resolution is `Unknown`, never absence. `Unknown` remains `Decided` without TTL and is retried by the reconciler, even after expiry.

If resolution confirms existing evidence after expiry or a fence/session failure, recovery records `EvidenceRecorded -> EvidenceWithheld`, a distinct terminal state that cannot expose a grant. No withdrawal fact is written: #99 must define that Chronicle contract and its relationship to connected-app visibility and terminal retention before live wiring. If resolution confirms sealed absence, recovery closes `Abandoned` without writing a fact. Pending requests that were never approved can expire directly.

A real authorization adapter must prepare the same **unusable** authorization on every retry. Every issuance and redemption must check the operational `Active` state and a live revocation fence; an authorization identity, successful preparation or replayed evidence alone is not authority. The production OpenIddict adapter and these protocol guards are not implemented here. Do not wire this flow into the live endpoint until the adapters and the #99/S6 fence contracts are implemented and verified.

## Storage and settings

MongoDB stores one majority-durable document per request in `consent-requests`, using the request identity as `_id`. Replacements compare identity, revision and observed state. Every pending approval/denial and Active write additionally requires `Deadline > now` in the atomic Mongo filter, using both the caller's UTC time and MongoDB's `$$NOW` execution time so a delayed write cannot rely on a stale eligibility check. The in-memory store models both clock conditions under its CAS lock. A compound account/session/realm/state/CreatedAt/deadline index supports the newest-pending lookup. A separate transition guard prevents changing the binding, creation time or recorded selection, approving without selection, denying with selection, or skipping the evidence barrier. MongoDB types are confined to `Cratis.Identity.ConsentFlow.Operational`.

`Identity:ConsentActivation` binds validated options:

| Setting | Default | Accepted range |
| --- | --- | --- |
| `ActivationDeadline` | `00:10:00` | Positive, at most one day |
| `TerminalRetention` | `30.00:00:00` | One to 365 days |
| `BatchSize` | `100` | 1 to 1000 |
| `ReconcileInterval` | `00:00:30` | One second to one hour |

TTL applies to `Denied`, sealed-absent `Abandoned`, and safe-terminal `EvidenceWithheld` rows only. Unknown evidence, pending decisions and Active grant-authority rows are not TTL-deleted. TTL is cleanup, not a deadline check. The reconciler scans with a bounded rotating identity cursor so an unknown oldest request cannot starve later requests, isolates per-request failures, and never claims code issuance. Logs contain only request identities and state, not bindings, session identities or credentials.

The real-kernel Chronicle replay acceptance scenario remains blocked on #99; the unit specs exercise the operational barrier and hostile crash windows with in-memory fakes. Integration projects are compiled with the solution but must not be executed locally for this work.

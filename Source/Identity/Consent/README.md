<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Operational consent activation

This is dormant infrastructure, not a replacement for the current authorization endpoint. It writes no Chronicle events and issues no OpenIddict authorization codes. The live consent flow is unchanged. Its namespace is `Cratis.Identity.ConsentFlow` to avoid colliding with the existing `Authorization.Consent` model.

`ConsentActivation.Create` accepts only already validated server-side authorization parameters. The caller must persist the resulting request identity in its protected browser flow, validate antiforgery and the authenticated session, and reconstruct the exact immutable binding on submission. Never use submitted client, realm, account, tenant, resource, scope or session values as that binding.

Approval compare-and-sets `Pending -> Decided`; denial closes the request without evidence. `Resume` idempotently records evidence, persists `EvidenceRecorded`, prepares a dormant authorization, rechecks the deadline and live fence, and compare-and-sets `Active`. Only `ClaimCode` can consume the single permission to issue a code. It requires the exact binding and a confirmed live fence. A crash after consuming that permission intentionally requires a fresh consent request, rather than issuing a second code. Actual code redemption and its single-use enforcement remain OpenIddict's responsibility.

## Deferred adapters

The default `IConsentEvidence` always returns `Unknown`, the default `IRevocationFence` denies, and the default `IAuthorizationActivator` returns no authorization. Thus the new path cannot reach `Active` with the shipped defaults, even if it is called directly. Applications may supply the eventual adapters before registration; registrations use `TryAdd`.

A real evidence adapter is blocked on #99. `Record` must deduplicate the complete evidence append by `ConsentRequestId`. `Resolve` must serialize against **both in-flight and future appends**. `NotRecorded` means no evidence exists and the identity has been permanently sealed against append. A partial append, unavailable writer or lost acknowledgement with no authoritative resolution is `Unknown`, never absence. `Unknown` remains `Decided` without TTL and is retried by the reconciler, even after expiry.

If resolution confirms existing evidence after expiry or a fence/session failure, recovery records `EvidenceRecorded -> EvidenceWithheld`, a distinct terminal state that cannot expose a grant. No withdrawal fact is written: #99 must define that Chronicle contract and its relationship to connected-app visibility and terminal retention before live wiring. If resolution confirms sealed absence, recovery closes `Abandoned` without writing a fact. Pending requests that were never approved can expire directly.

A real authorization adapter must prepare the same **unusable** authorization on every retry. Every issuance and redemption must check the operational `Active` state and a live revocation fence; an authorization identity, successful preparation or replayed evidence alone is not authority. The production OpenIddict adapter and these protocol guards are not implemented here. Do not wire this flow into the live endpoint until the adapters and the #99/S6 fence contracts are implemented and verified.

## Storage and settings

MongoDB stores one majority-durable document per request in `consent-requests`, using the request identity as `_id`. Replacements compare identity, revision and observed state. A separate transition guard prevents changing the binding or skipping the evidence barrier. MongoDB types are confined to `Cratis.Identity.ConsentFlow.Operational`.

`Identity:ConsentActivation` binds validated options:

| Setting | Default | Accepted range |
| --- | --- | --- |
| `ActivationDeadline` | `00:10:00` | Positive, at most one day |
| `TerminalRetention` | `30.00:00:00` | One to 365 days |
| `BatchSize` | `100` | 1 to 1000 |
| `ReconcileInterval` | `00:00:30` | One second to one hour |

TTL applies to `Denied`, sealed-absent `Abandoned`, and safe-terminal `EvidenceWithheld` rows only. Unknown evidence, pending decisions and Active grant-authority rows are not TTL-deleted. TTL is cleanup, not a deadline check. The reconciler scans with a bounded rotating identity cursor so an unknown oldest request cannot starve later requests, isolates per-request failures, and never claims code issuance. Logs contain only request identities and state, not bindings, session identities or credentials.

The real-kernel Chronicle replay acceptance scenario remains blocked on #99; the unit specs exercise the operational barrier and hostile crash windows with in-memory fakes. Integration projects are compiled with the solution but must not be executed locally for this work.

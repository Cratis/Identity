# Delegation

```text
Spec id: arc-delegation
Version: 1
Status: Signed off
Signed off: 2026-11-05 by woksin, https://github.com/Cratis/Identity/issues/14; owner confirmation pending
Issue: https://github.com/Cratis/Arc/issues/3702
Sign-off issue: https://github.com/Cratis/Identity/issues/14
Supersedes: None
```

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Purpose

Delegation lets an authenticated agent act for a person without inheriting unrestricted person authority. A server-side lineage bounds every descendant. This specification does not claim delegation or approval support is already shipped.

## Terms

Person, Account, Realm, Issuer, Subject, Linked key, Resource, Resource indicator, Audience, Tenant, Organisation, Environment, Actor, Credential kind, Assurance, Policy epoch, Access token, ID token, Refresh token, and PAT have exactly the meanings in the Terms table of [identity-contract-v1.md](identity-contract-v1.md), Cratis/Identity#24. Shared meanings are not redefined here.

A **grant** is the server-side record of delegated authority and bounds. A **root grant** records the original consent; a **child grant** names its parent. A **budget** limits calls or writes. A **reservation** moves available budget from a parent to a child without increasing the root total. An **idempotency key** identifies one allocation, consumption, or release and its exact request. A **prepared operation** is the separately approved canonical operation required when operation policy demands approval.

## Normative contract

MUST, MUST NOT, and SHOULD use [RFC 2119](https://www.rfc-editor.org/rfc/rfc2119) meanings. Requirements apply after sign-off.

### Identity and grant record

A delegated token MUST retain the represented person's lowercase UUID v4 account subject as `sub`, with the realm `iss`, as defined in identity-contract-v1. It MUST identify the current agent through the registered `iss` and `sub` strings in the `act` object. Nested historical `act` claims MUST be informational only, not authorization inputs. Authorization MUST use the current actor, validated top-level claims, and the server-side record named by `grantId`. A token or grant id MUST NOT substitute for authenticating the current actor.

The grant reference MUST come only from the verified credential or a server-side mapping keyed by the verified `credentialId`. Request headers, body, or query MUST NOT supply or override it; a request-supplied reference MUST be ignored or refused. If the verified credential maps to `grant-a` and the request supplies `grant-b`, authorization MUST use only `grant-a` or refuse the request; `grant-b` bounds MUST NOT be read. The grant record's `accountKey`, current `actor` (`iss`, `sub`), `resource` and catalog audience, `tenant`, and `environment` MUST equal the verified token's corresponding values and trusted installation binding. Any mismatch MUST cause refusal.

Every delegation and exchanged token MUST reference a server-side record containing:

- `grantId`, `rootGrantId`, `parentGrantId` (absent only at the root), and `depth`;
- `accountKey` (`issuer`, `subject`), product `person` reference, `actor` (registered `iss`, `sub`), `authorizedExchangeActors`, and task identifier `taskId`;
- `allowedDestinations` from root consent, current `resource`, catalog `audience`, `tenant`, and `environment`;
- resource-qualified `scopes`, carried `assurance` (`idp`, `auth_time`, `acr`, optional `amr`), and applicable nonnegative integer `policyEpoch`;
- required `recoveryEpoch`: the authority/realm recovery epoch the grant was created under, as defined in [identity-revocation-v1.md](identity-revocation-v1.md) §6;
- `rootExpiresAt`, `expiresAt`, budgets, remaining amounts, reservations, and lifecycle/revocation state.

An agent acting for a person MUST have a person-bound grant. An organisation automation credential MUST NOT substitute for that delegation. Support-session impersonation MUST remain separate from person-requested agent work. Actor attribution MUST distinguish an authenticated actor from a public client's declared `client_id` or PAT label.

### Attenuation and revocation

Each child MUST satisfy `child.allowedDestinations ⊆ parent.allowedDestinations ⊆ root.allowedDestinations`; its selected destination MUST be in those sets. Root consent MUST NOT restore a destination removed by a parent. For a same-resource child, its scopes MUST be a subset of the parent's scopes. Cross-resource scope attenuation MUST use explicit [catalog mappings](arc-scope-catalog-v1.md), never string subsets. Tenant, environment, and assurance MUST be no wider than the parent and root. Carried assurance MUST NOT be refreshed merely by exchanging a token. `auth_time` MUST remain the verified original NumericDate; unknown assurance MUST remain `acr=basic`, not become stronger evidence. Child expiry MUST be no later than either the parent's expiry or `rootExpiresAt`.

The authenticated actor MUST be in the parent's `authorizedExchangeActors` before exchange; any other actor MUST be refused. Depth MUST be capped; the decided cap is two edges, root person grant at depth 0, first agent at 1, downstream agent at 2. Depth 3 MUST be refused. Products MUST configure explicit finite call, write, and lifetime bounds before enabling agent work; q15 leaves the numeric defaults to per-product configuration.

Revoking any ancestor MUST close all descendants under the agreed revocation protocol. Every resource MUST refuse a descendant within the revocation bound of the W0 revocation contract ([identity-revocation-v1](identity-revocation-v1.md), Cratis/Identity#32); this spec MUST NOT imply immediate fleet-wide propagation. Exchange MUST check every ancestor's fence. Resources retaining long-lived grants MUST check the chain. An unavailable required lineage or fence check MUST cause refusal, not trust in nested token history.

### B5/C4: budget atomicity and retries

The parent debit and child creation MUST occur in one transaction. A child budget MUST be reserved from the parent's available remainder; siblings and direct parent spending MUST NOT exceed root limits. Consumption MUST check grant lifecycle state, MUST be refused when the grant is closed or revoked under the agreed revocation bound, MUST be atomic at the consuming resource, and MUST NOT exceed the assigned budget.

Every allocation, consumption, and release MUST carry an idempotency key. Repeating a key with the same request MUST NOT unconditionally return the existing outcome. Before replaying, the resource MUST check the grant's recovery epoch against the current authority/realm recovery epoch from independent custody, the grant's lifecycle state, and every ancestor, per [identity-revocation-v1.md](identity-revocation-v1.md) §6 and §6.1. A record from an older recovery epoch, or with the epoch missing or unavailable, MUST be denied and never replayed or re-evaluated; a closed or revoked grant or ancestor MUST be refused. Only when those checks pass MAY the existing outcome be returned, without a second debit or release (vector: identity-revocation-v1 `restore_delegation_budget.json`, order 8). Reusing a key with a different request MUST be refused. Idempotency keys MUST be scoped to the grant and operation so unrelated callers cannot alias one another's accounting.

A release MUST first close the descendant and settle in-flight spending; unused reservations MUST NOT return while descendant spending is still possible. A crash between debit and child creation MUST leave neither effect, or recover the committed pair through the same idempotency key. Ancestor revocation MUST close descendants before their reservations can be reclaimed. Accounting MUST preserve consumed amounts rather than replenish them on revocation.

### Authorization and approvals

The receiving resource MUST intersect product policy with the grant, credential scopes, tenant, environment, time, usage, and assurance requirements. Bounds MUST only narrow authority. Audit records MUST retain person, current actor, declared client, protocol, tenant, credential id, and grant id without secrets.

Where operation policy requires approval, execution MUST also satisfy the separate prepared-operation approval contract, including execution-time rechecks and single consumption. An MCP elicitation response MUST NOT count as approval. This grant contract MUST NOT imply that every write needs approval or that a grant bypasses a required approval.

## Test vectors

Synthetic root `root-fixture` permits Direct and Studio, tenant `tenant-fixture`, environment `test`, and expires at `2030-01-01T01:00:00Z`. Additional audiences are not consented.

| Input | Expected result |
| --- | --- |
| Root → Studio child → Atlas token exchange, Atlas absent from root destinations | Refused despite intermediate Studio authority. |
| Verified token `act=agent-B`; referenced grant has `actor=agent-A` | Refused for credential/grant mismatch. |
| Authenticated exchange actor absent from parent's `authorizedExchangeActors` | Refused. |
| Grant reference supplied in a request header, body, or query rather than verified credential/mapping | Refused; request input never selects a grant. |
| Verified credential maps to `grant-a`; request supplies `grant-b` | Refused; request reference unused, `grant-b` bounds never read. |
| Root permits Direct and Studio; parent narrows destinations to Studio; child requests Direct | Refused; root consent does not restore a removed destination. |
| Same-resource parent has `studio:read`; child requests `studio:write` | Refused; child scopes must be a subset. |
| Depth-2 agent exchanges to a depth-3 child | Refused under cap 2. |
| Parent has 10 calls; child A reserves 7, child B requests 4 | B refused; root total cannot reach 11. |
| Parent has 5 calls; two concurrent child requests each reserve 4 with different keys | Exactly one succeeds; remaining parent budget is 1. |
| Allocation key `alloc-fixture` repeated with identical 4-call request | Same child and outcome; debit occurs once. |
| Same key with a 5-call request | Refused as conflicting retry. |
| Consumption key `call-fixture` repeated; release key `release-fixture` repeated | Each effect occurs once; neither double-spend nor double-credit. |
| Crash between parent debit and child creation | Transaction rolls back both, or committed pair is recovered by key; no orphan debit. |
| Child still has in-flight spending when release requested | No release until closure and settlement; no extra spendable budget. |
| Middle ancestor revoked; descendant token unexpired | Refused within the revocation bound in Cratis/Identity#32. |
| Closed grant attempts budget consumption | Refused; no debit. |
| Nested `act` claims name a privileged former actor; current actor lacks permission | Refused; prior actors do not authorize. |
| Child requests another tenant/environment, later expiry, or stronger assurance | Refused. |
| `direct:read` mapped to Studio write solely by suffix or string subset | Refused; an explicit reviewed mapping is required. |

Machine-readable group: [lineage and budgets](../../Source/Identity.Specs/Vectors/arc-delegation-v1/lineage-and-budgets.json), repository path `Source/Identity.Specs/Vectors/arc-delegation-v1/lineage-and-budgets.json`. The group has `name`, `description`, `given`, `when.action`, and `then`; `given.vectors` contains individual cases. These are data vectors, not executed runtime conformance tests. Outcomes and reasons are vector vocabulary, not public error messages.

## Decisions recorded at sign-off

Decided in Identity #14 (2026-11-05) with the recommended answers from the sign-off package:

- **q15:** numeric default call, write, and lifetime bounds are per-product configuration and are not specified here. No unlimited fallback is permitted.
- **B3:** the depth cap of 2 and the grant persistence/revocation design are decided.
- **W0 / Cratis/Identity#32:** the revocation bound is specified in [identity-revocation-v1](identity-revocation-v1.md); descendant refusal and lifecycle enforcement follow that contract.
- **N1 / q9:** cross-resource mappings are locked with the resource owners; similar names are not mappings.
- **D6:** person-requested agent work requires delegation; organisation automation remains separate. Which interim tools meet read-only containment is q12 and is outside this contract.
- **(align with #24):** #24 fixes person UUID subjects, `act` object shape, assurance fields, and nonnegative policy epochs, but leaves actor registration namespaces open. The grant-record schema, root/parent references, budgets, and depth are additional Arc mechanics. A signed `grantId` token claim is not in #24's access-token allowlist and MUST NOT be added without coordinated claim-profile review/versioning; the record reference stays a record reference, not a token claim. `jti` MUST NOT be treated as a grant id.

## Versioning

This spec MUST change only through a new version file. Unknown grant versions MUST be refused. A change to depth, destinations, mappings, budgets, or expiry MUST NOT widen an existing grant silently; any broader authority MUST require new consent and current product authorization.

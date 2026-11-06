# Identity platform corrections

```text
Doc id: identity-platform-corrections
Version: 1
Status: Proposed
Issue: https://github.com/Cratis/Identity/issues/11
Depends on: identity-design v1 (Signed off), identity-module-outline v1, identity-tenant-model v1 (Signed off), arc-delegation v1 (Signed off), identity-events v1 (Signed off)
```

## 1. Scope and authority

This note records four corrections to the platform design: how a customer's identity provider is federated (C1), who owns which grant (C2), how consent and grant creation commit (C3), and how an approved operation relates to its external effect (C4). It is a design note. It adds no event, claim or contract, and it does not edit a signed-off specification. Where a correction contradicts a signed-off specification, section 6 lists the conflict as an open item, and the specification stays as signed until the owners resolve it in [Identity #14](https://github.com/Cratis/Identity/issues/14).

The [module outline](identity-module-outline-v1.md#9-corrections-adopted-in-the-outline) applies C1 to C3 to the delivery steps, and S4 carries the C3 acceptance criteria.

## 2. C1: federation through a Cratis issuer, no header-derived tenant binding

A customer's identity provider (Entra ID, Okta, Google Workspace; OIDC first, SAML later) is connected as an **upstream of a Cratis Identity realm**. This is the default "bring your own identity provider" profile, for hosted and self-hosted installs. The realm issues the tenant- and resource-bound credential (`tid`, `aud`) after tenant selection and a live membership check.

A **direct external authorization server mode** is allowed only when the external token itself carries an immutable tenant and resource constraint that the installation verifies through configured, issuer-scoped claim mapping, for example a per-tenant audience or app registration. Otherwise AuthProxy exchanges the external token at tenant selection for a locally issued, tenant-bound session, and bearer routes refuse raw external tokens.

A request header, route or tool argument only selects the target. It is never a credential constraint.

Why: a person who belongs to tenants A and B could replay one external token across both, and a membership check does not contain the credential to one tenant. Federating through the realm makes the refusal of raw external tokens meaningful for every identity provider.

## 3. C2: grant ownership split

**Identity** is the only writer of **issuer grants**: OAuth consent and authorization records, refresh families, revocation fences and, later, RFC 8693 exchange-lineage records for tokens it issues.

The **product** is the only writer of **task authority**: a task delegation for its own agent (actor, represented person, task, product-qualified scopes, resource, environment, usage and time bounds), prepared operations, approvals, execution records and usage counters. It writes these in its own event store through Arc building blocks.

- **Protocol.** A task delegation names its parent credential (session, PAT or OAuth grant id), taken from the verified caller. At creation and on every use, the product checks the parent through the issuer's revocation status within the revocation bound. Identity does not see product tasks.
- **Version 1 is one hop:** person to that product's agent, same resource, no exchange.
- **Budgets are product-local** and counted atomically in one store. Cross-product exchange, nested budgets and parent-debit reservations are **deferred**, with the identifiers `grantId`, `parentGrantId`, `rootGrantId` and `taskId` reserved.

Why: the earlier design assigned the same record to two writers, and RFC 8693 supplies no cross-product atomic budget.

## 4. C3: consent and grant creation commit through an activation barrier

Grant creation is a durable state machine in the operational store. That store is the single authority for every transition, and each transition is a compare-and-set.

| Step | Transition | What it does |
|---|---|---|
| 1 | `Pending(ConsentRequestId, session)` to `Decided(approve\|deny)` | The single-use point. A second decision on the same request finds it decided and changes nothing. |
| 2 | `Decided` to `EvidenceRecorded` | `ConsentGranted` and `AuthorizationGrantRecorded` are appended to Chronicle with a deterministic event source derived from `ConsentRequestId`. The append is idempotent. |
| 3 | `EvidenceRecorded` to `Active` | Only now does the operational authorization become usable and only now is an authorization code issued: the **activation barrier**. Activation also checks the revocation fence. |

- A reconciler resumes any request stuck in `Decided` or `EvidenceRecorded`.
- If activation can no longer happen (the request expired, the session ended or a fence was set), the reconciler appends the existing withdrawal fact with a system cause and the reason `ActivationAbandoned`, so connected apps never show a grant that was never usable. Whether the signed-off catalogue allows a system-caused withdrawal is an open item (section 6); if it does not, a new `AuthorizationGrantAbandoned` event is the alternative.
- A deny appends nothing usable.

No usable grant exists before durable consent evidence. This keeps the operational status authoritative ([package and storage design §2](identity-package-and-storage-v1.md)), because live status must change atomically and expire, and adds the barrier that creation lacked. Revocation already has a fence-and-outbox protocol ([revocation](../specs/identity-revocation-v1.md)); creation now has an equivalent.

**Crash-window tests (S4 acceptance).** The required specs are listed in the [outline, section 9](identity-module-outline-v1.md#s4-acceptance-the-grant-activation-barrier).

## 5. C4: approved intent is separate from external execution

Consuming an approval and creating an **execution record** (execution id, the canonical prepared operation, the destination idempotency key or precondition, state `Dispatching`) commit atomically in the product's store. "Exactly once" is promised for this local commit only.

A dispatcher performs the external effect through the destination's idempotency key or a conditional operation, and records one of three outcomes:

- `Succeeded`;
- `Failed`, which is definitive, so the person may prepare the operation again;
- `OutcomeUnknown`, for a timeout, a crash after send or an ambiguous response.

`OutcomeUnknown` triggers reconciliation by reading the destination's state. A write is never retried blindly. If reconciliation cannot decide, the state stays `OutcomeUnknown`, is shown to the person, and a retry needs a new approval.

The mapping for GitHub in Studio:

| Operation | Rule |
|---|---|
| Push | Force-with-lease on the expected old remote ref, with the approved head SHA |
| Open a pull request | Find the open pull request by head, then create |
| Merge | The merge API with `sha` set to the approved head, which refuses if the head moved |
| Reconcile | Read the ref, the pull request and the merge commit |

Why: a local transaction cannot make a push or a merge happen exactly once.

## 6. Open items: conflicts with signed-off specifications

These specifications are Signed off and are not edited here. Each item needs an owner decision in [Identity #14](https://github.com/Cratis/Identity/issues/14), after which the specification gets a new version.

| Specification | Where | Conflict with |
|---|---|---|
| [identity-tenant-model-v1](../specs/identity-tenant-model-v1.md) | §6 credential constraints and §7 step 1 | C1: neither states that a raw external token is refused unless it carries an immutable tenant and resource constraint, nor that the realm is the default federation point. |
| [arc-delegation-v1](../specs/arc-delegation-v1.md) | "Identity and grant record" and "B5/C4: budget atomicity and retries" | C2: the grant record (`rootGrantId`, `parentGrantId`, `depth`, exchange actors) and the parent-debit transaction describe a cross-product tree at the issuer. Under C2 the task record is product-owned, version 1 is one hop and budgets are product-local. The nested model becomes deferred. |
| [arc-delegation-v1](../specs/arc-delegation-v1.md) | "Authorization and approvals" | C4: it requires single consumption of an approval but says nothing about the execution record or the `OutcomeUnknown` state. |
| [identity-events-v1](../specs/identity-events-v1.md) | §8 `ConsentGranted` and `AuthorizationGranted`; §4.1 source facts | C3 proposes one event source derived from `ConsentRequestId`, but §8 places `ConsentGranted` on Account and `AuthorizationGranted` on Grant. Section 4.1 requires `AuthorizationGrantRecorded` to retain the public contract's Grant event source. The proposed shared stream conflicts with these signed-off placements; stream placement requires owner resolution before S4 implementation and does not override the specification. |
| [identity-events-v1](../specs/identity-events-v1.md) | §8 `ConsentWithdrawn` | C3: it carries `ReasonId` and is written for a person's withdrawal. It is not stated whether a system-caused withdrawal with the reason `ActivationAbandoned` is allowed. |
| [identity-design](identity-design.md#who-writes-which-fact) | "Who writes which fact" | C2: the row "Delegations, grants for agents, approvals" says "grants" without distinguishing issuer grants from task authority. The binding excerpt stays as signed. |

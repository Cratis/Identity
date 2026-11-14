# Access freshness and policy epochs

```text
Spec id: identity-access-freshness
Version: 1
Status: Draft
Issue: https://github.com/Cratis/Identity/issues/33
Sign-off issue: https://github.com/Cratis/Identity/issues/14
Supersedes: None
```

## 1. Scope and decision basis

This draft defines when Identity, AuthProxy and Arc may use local Access projections for policy decisions. It specifies source progress, ordinary and critical freshness tiers, and the epoch-before-policy invariant. It claims no implemented enforcement or measured latency.

The [identity contract](identity-contract-v1.md), [revocation specification](identity-revocation-v1.md), [verified caller](arc-verified-caller-v1.md), [forwarded identity v2](arc-forwarded-identity-v2.md) and [tenant model](identity-tenant-model-v1.md) remain authoritative. The [design ownership table](../design/identity-design.md#who-writes-which-fact) and D20, [Identity #69](https://github.com/Cratis/Identity/issues/69), option C supply the decision basis. D20 requires a 60-second ordinary bound, a heartbeated local priority fence with a 5-second critical target, refusal while projections lag, and measured partition/recovery tests before release. Sign-off of this draft remains in #14; #69 does not sign it off. The separate 2026-11-08 edge revocation decision in #69 is recorded in [resource token validation](../security/resource-token-validation.md); introspection and its cache are not substitutes for Access freshness or authoritative next-use checks.

Terms retain the meanings in [contract Terms](identity-contract-v1.md#terms). A **source watermark** is authenticated evidence of Access source progress for the applicable policy scope. An **Access sequence** orders source facts in that scope. A **projection position** is the highest contiguous applied sequence together with its applied policy epoch. A **priority fence** is authenticated, source-originated evidence of the minimum Access sequence and policy epoch required for a critical decision. These are logical values, not new claims, headers or wire formats.

## 2. Authority boundaries

Access alone writes organisation identity, organisation policy, organisation policy epoch and confirmed tenant bindings. Each product alone writes tenant membership, roles and business authorization. Identity writes accounts, linked identities, consent and grants, and advances realm policy epochs through `RealmPolicyEpochAdvanced` ([event catalogue](identity-events-v1.md#8-event-catalogue-v1)). Consumers MUST NOT turn an Access projection, an organisation administrator role or a tenant list into membership authority. Products MUST check live membership and roles on every request; unknown membership MUST deny. The [tenant model](identity-tenant-model-v1.md#3-ownership-d5--a) defines those boundaries.

Credential claims, issuance-time policy evidence, a forwarded signature and matching epoch numbers MUST NOT independently authorize a request. Resource, tenant, environment, audience, scope, assurance, sender binding, credential status and recovery checks still apply. Freshness MUST NOT widen a credential's constraints. This specification adds no per-request live Access call: consumers use local projections plus authenticated source progress and the local priority fence. A missing required local check MUST deny, not fall back to claims or a cached positive authorization.

## 3. Source progress and age

A consumer MUST retain, per applicable source scope:

- The last trusted source watermark, its source progress time and the sequence it covers.
- The contiguous applied projection sequence and policy epoch.
- For critical decisions, the latest trusted priority fence, its required sequence and epoch, and the time of its source heartbeat.

A watermark MUST prove progress at the source, including when there are no policy changes. Local event receipt, queue activity, duplicate delivery, projection rebuild activity or receipt of a delayed old event MUST NOT reset source freshness. Neither a watermark nor a heartbeat applies an event to the projection. Consumers MUST establish that the projection covers the watermark used for an ordinary decision; gaps, untrusted progress and unknown positions MUST deny.

Age is elapsed time since trusted **source progress**, not elapsed time since local receipt. The implementation MUST account conservatively for transport delay and clock uncertainty, using a monotonic elapsed-time basis after validating source evidence; restarting a process MUST NOT renew old evidence. If an upper bound on age cannot be established, freshness is unknown and MUST deny. Source positions MUST NOT regress on replay. A recovered or rebuilt projection MUST prove its contiguous applied position before use.

The exact source evidence transport, clock-error allowance and partition measurement harness remain implementation/sign-off work. They MUST NOT enlarge the bounds below or treat arrival time as proof of freshness.

## 4. Two freshness tiers

The limits below are inclusive: age equal to the bound passes the age check; age greater than the bound MUST deny. Passing an age check alone never authorizes.

| Tier | Decisions | Required local evidence |
|---|---|---|
| Ordinary | Policy-dependent reads | Trusted source-progress age at most 60 seconds and a projection covering that watermark; current policy evaluation and all other checks |
| Critical | Approval use, policy tightening, step-up/assurance decisions, delegation grants, and other critical operations under revocation §8 | Ordinary evidence plus a priority heartbeat age at most 5 seconds and a projection at or beyond **both** the fence's Access sequence and policy epoch |

Every policy-dependent operation not explicitly classified as Ordinary MUST be treated as Critical. Policy-dependent issuance, refresh and continuation MUST enforce the applicable tier and MUST NOT carry an old policy epoch forward without current-policy re-evaluation, per [revocation §8](identity-revocation-v1.md#8-dependency-down-behavior). An operation MUST NOT be downgraded to an ordinary read to evade critical checks. Restrict-only owner-authorized revocation retains revocation §8's exception: no stale policy may authorize new privilege or weaken a restriction.

Critical decisions MUST deny if either progress or heartbeat is unknown, stale or untrusted, or if the required projection is unavailable or behind. A fresh heartbeat never overrides a behind projection. Ordinary reads may continue while the critical heartbeat is stale only if their own source progress, projection and authorization checks remain valid.

The 60-second and 5-second figures are D20 targets pending measurement, not observed service levels. Implementations MUST fail closed at these limits. Measurement and partition/recovery tests are release preconditions (§8), not reasons to relax a limit during an outage.

## 5. Epoch before policy (C2)

For a critical decision, let the trusted fence require sequence `S` and epoch `E`. The consumer MUST refuse until its contiguous projection has applied sequence **at least S** AND policy epoch **at least E**. Checking just one coordinate is insufficient. Equal epochs with a lagging sequence MUST deny; a projection at the required sequence but at an older epoch MUST also deny. Any inconsistency or unknown coordinate MUST deny.

The consumer then evaluates the operation under the applied current policy and all live product checks. Catch-up permits re-evaluation, not automatic approval. A decision MUST NOT commit against an older position if its required fence advances before the decision's authorization point. Implementations MUST test that ordering, not read a heartbeat once and assume it stays authoritative indefinitely.

During a partition, ordinary decisions cease when trusted source progress exceeds 60 seconds; critical decisions cease when the source heartbeat exceeds 5 seconds, or sooner if either projection coordinate is behind. On reconnection, resuming heartbeats is insufficient: the projection MUST catch up to both required coordinates before any critical decision. Replaying old events MUST NOT renew freshness.

## 6. Policy epochs on credentials

Sessions, PATs, refresh credentials/families and grants MUST record the applicable policy epoch at creation. Each use or continuation that depends on organisation policy MUST compare that evidence with the current locally proven policy position and evaluate current policy. An older credential epoch MUST force re-evaluation; it MUST NOT be trusted or silently copied into a renewed credential as current authority. If re-evaluation refuses, required policy is unavailable, or freshness cannot be proven, the operation MUST deny. Re-evaluation MUST NOT widen consent, scopes, resource, tenant, environment or assurance.

A realm policy epoch reported by `RealmPolicyEpochAdvanced` MUST follow the same current-policy re-evaluation and fail-closed rules. An Access organisation watermark or matching organisation epoch MUST NOT prove realm-policy freshness. The authoritative realm-source progress and applied realm epoch/fence MUST be proven fresh before a realm-policy-dependent decision; the mechanism for that proof remains outstanding in §8. If that proof is unavailable or unknown, the decision MUST deny.

For a critical operation the projection must first meet §5. For an ordinary operation it must meet §3–4. An epoch matching the credential is not a substitute for those checks. A credential epoch ahead of an unproven local projection MUST NOT establish current policy.

A **recovery epoch is different**. Under [revocation §5](identity-revocation-v1.md#5-invariant-b1-acknowledged-fences-survive-restore) and §8, a credential from an older recovery epoch MUST always deny. Current policy MUST NOT rescue it, re-evaluation MUST NOT upgrade its recovery epoch, and no policy result may clear a tombstone. Revocation's independent recovery-epoch authority and credential-status fences remain mandatory.

## 7. Data vectors

[JSON vectors](../../Source/Identity.Specs/Vectors/identity-access-freshness-v1/) are data only, not implemented or passing executable conformance tests. Each file contains `name`, `description`, `given`, ordered `when` steps, and `then`. Times are synthetic seconds on a trusted elapsed-time basis with no clock uncertainty; fixtures use logical positions, not wire claims. Outcomes/reasons are fixture vocabulary, not public error messages. Unspecified dependencies are healthy, credentials have current recovery epochs and valid constraints, membership is live/allowed, and current policy allows unless stated otherwise. `allow_after_checks` means the described operation passes only with those other checks; it is not permission from freshness alone.

The corpus covers N+1 fences ahead of policy, each lagging coordinate independently, catch-up, 60/5-second boundaries, stale critical versus still-fresh ordinary decisions, delayed old events, partition recovery, older credential policy epochs, and unconditional old-recovery denial. A harness MUST also test unknown evidence, restart/clock uncertainty, gap detection, fence advancement during decisions, and all dependency failures in revocation §8. Parsing these files proves syntax only.

## 8. Outstanding decisions and release preconditions

- Specify how Identity's authoritative realm-policy source progress and the applied `RealmPolicyEpochAdvanced` epoch/fence are proven fresh under the applicable tier, including their relationship to Access watermarks and priority fences. Until that proof is available, realm-policy-dependent decisions MUST deny; organisation evidence alone is insufficient.
- Measure source-to-consumer progress and heartbeat age, including clock uncertainty, across Identity, AuthProxy and Arc; demonstrate the 60-second and 5-second targets without local-receipt resets.
- Specify and execute partition/recovery tests: stop each feed, delay/replay events, pause/rebuild a projection, resume heartbeats before projection catch-up, and advance fences at the authorization boundary. Prove refusal until both coordinates are applied and live membership is known.
- Obtain #14 sign-off for this draft after the evidence required by #69. No runtime conformance or release readiness is claimed here. Consumer work remains [Cratis/Arc#3708](https://github.com/Cratis/Arc/issues/3708) and [Cratis/AuthProxy#255](https://github.com/Cratis/AuthProxy/issues/255).

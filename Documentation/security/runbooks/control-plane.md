# Recovery referral: control plane

Status: Draft coordination referral only. Owner: Infrastructure incident authority with accountable issuer/security owner; **woksin holds the role (Identity #20)**. Control-plane hardening and cluster restoration are outside Identity's scope. Recovery is **cluster-level**, not an Identity-only rotation. Refer to [Cratis/Infrastructure](https://github.com/Cratis/Infrastructure) for the infrastructure-owned incident response; an approved cluster-level runbook link is still needed before operational use. The [matrix](../compromise-matrix-v1.md) does not claim per-app or per-realm containment inside a compromised administrative boundary.

## Trigger and detection

Cluster-admin, CI deployment or secret-store administrator compromise; unexpected privileged access, key-policy or deployment changes. Notify the Infrastructure authority with external audit evidence and earliest plausible exposure; assume all reachable cluster keys, hosts and credentials are affected until that authority bounds the incident. In-cluster logs and backups may be untrustworthy.

## Immediate containment (ordered handoff)

1. Escalate to the Infrastructure incident authority via the cluster-level response; identify a trusted administrative and communications boundary outside the suspected compromise.
2. Request cluster-wide containment and denial of affected identity/resource traffic under that authority, preserving independently held evidence. Rotating keys under compromised administration cannot establish recovery.
3. Keep Identity issuance/redemption and affected resource readiness denied until Infrastructure establishes a clean recovery authority and hands over a verified inventory. Do not use this referral as a cluster rebuild procedure.

## Key and credential rotation handoff

Infrastructure must coordinate the inventory/order for administrator, CI, secret-store, cluster/workload, TLS, Chronicle/store, realm signing/encryption/session, edge and product credentials. Establish clean administration/custody first; Identity then follows [all affected host recovery](shared-identity-host.md) and [AuthProxy recovery](authproxy-process.md) for replacement trust, signing and consumer verification before reopening. **No acceptance overlap** for compromised credentials; overlap of unaffected trust is determined by the infrastructure plan and deployed lifetime/cache bounds, not invented here. Scope includes all keys/credentials the compromised administrators could reach.

## Revocation handoff

Request cluster-wide coordination of sessions, PATs, OAuth grants/refresh families, delegation, runners and approvals across **every affected realm and product authority**. Apply [revocation §§3, 6–8](../../specs/identity-revocation-v1.md) only from trusted recovered authority: atomic fences/outboxes, independently durable acknowledgement, trusted monotonic epoch invalidation and verifier-wide enforcement. Validate independent fence-log custody and restore/consumption coverage; if those are compromised or cannot be proven, remain denied pending an approved recovery strategy. Realm and app separation do not contain administrator compromise; offline JWT expiry alone is not full credential recovery.

## Audit review handoff

Infrastructure leads review of external cluster, CI, secret-store and break-glass records for the full exposure window. Identity/product owners correlate issuance, session, PAT/grant, realm-store, fence/outbox and downstream authorization/data audit using non-secret ids. Look for tampered recovery evidence, persistence, key exports, unauthorized deployments and cross-realm access. Do not trust same-cluster evidence as the sole proof of containment.

## Communication

Notify Infrastructure incident authority, accountable issuer/security owner, all affected component/product/realm owners and recovery/on-call/privacy roles through trusted channels. The accountable owner role coordinates the customer notification decision with privacy/security and affected owners; this referral makes no customer notification decision.

## Verification and reopening handoff

Infrastructure must establish clean administration, custody and recovery evidence before Identity verification has meaning. Then require all affected host/edge/product runbooks' old-key and old-credential denial, trusted fences/epochs/watermarks, fresh authorized access and cross-app/realm isolation checks across replicas. Cluster-level approval and the accountable Identity owner's acceptance are prerequisites; a single successful login or rotated signing key is not proof of recovery.

## Rehearsal

**rehearsal issue needed** for cluster-wide compromise/recovery and an approved Infrastructure runbook link. [Cratis/Infrastructure #294](https://github.com/Cratis/Infrastructure/issues/294) (edge keys) and [#299](https://github.com/Cratis/Infrastructure/issues/299) (products-realm restore/signing keys) are linked sub-drills, not a full control-plane recovery rehearsal.

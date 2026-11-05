# Recovery: AuthProxy process

Status: Draft. Owner: AuthProxy maintainer with key custodian, product credential authorities and recovery/on-call role; **owner role pending #20**. Follow the [shared rules](../compromise-matrix-v1.md#recovery-rules-shared-by-the-runbooks) and [revocation §§3, 6–8](../../specs/identity-revocation-v1.md). Scope is every app fronted and credential reachable during the window, not just one edge key.

## Trigger and detection

Unexpected signing/session activity, secret reads, image changes, TLS identity use, egress or cross-tenant assertions. A compromised AuthProxy can emit valid envelopes and impersonate privileged existing members; honest product checks do not make this harmless. Determine earliest possible compromise, routes, replicas, isolation tier and transit credentials. [DPoP](../../specs/identity-dpop-v1.md) does not repair a compromised enforcing edge.

## Immediate containment (ordered)

1. Stop forwarding from all suspect replicas and deny affected identity-bearing routes at independent app/ingress boundaries; preserve independent evidence.
2. Disable its workload and secret-store access, signing permissions and TLS identity. Remove all exposed edge key ids from app verifiers; do not rely on the compromised edge to revoke itself.
3. Inventory every fronted app and session/grant/PAT seen or potentially seen. If the window or logging is incomplete, assume all reachable credentials in that scope. Recover a patched clean deployment under a trusted control plane; escalate issuer/admin exposure if found.

## Key and credential rotation

Generate fresh per-app HMAC/ES256 keys in clean custody, distribute only needed verifier material, then enable clean signers while routes remain denied. Replace AuthProxy TLS identity, workload/secret-store credentials, session-protection keys and readable OAuth client/operational credentials. Disable old credentials everywhere; **no acceptance overlap** for compromised keys, session protection or TLS identity. Inventory shared replicas so none can keep old acceptance alive. Preserve protocol compatibility; no unsigned or Bearer downgrade. Issuer keys are rotated only if exposed, via the host runbook.

## Revocation actions

Fence all affected app sessions and OAuth authorizations/refresh families, delegation descendants, approvals and runner/on-behalf sessions exposed in the window. Revoke every PAT seen or potentially seen through its product-local authority, not just Identity. Advance applicable trusted app/grant epochs, or realm epochs if required by available safe invalidation and agreed scope. Require durable fence/outbox and independent log acknowledgement; verify outbox progress, every downstream status/epoch check and restore barriers. Re-authenticate affected users; access-token expiry alone is not sufficient for refresh/session/PAT recovery. Keep routes denied until offline-token rejection/expiry is proven under the [token profile](../../specs/identity-access-token-v1.md).

## Audit review for the exposure window

Review independent KMS/secret-store, workload, ingress and deployment audit; signing and session records; Arc/product authorization, tenant and membership changes; token/grant issuance and PAT use at product authorities. Look for privileged impersonation, replay, credential capture, cross-app key reuse, data exfiltration and unauthorized persistent grants. Treat AuthProxy-local logs as suspect; retain non-secret correlations only.

## Communication

Notify accountable issuer/security owner, AuthProxy/Arc and Identity maintainers, key custodian, all fronted product owners and recovery/on-call/privacy roles. Communicate scope, blocked routes and revocation gaps. The owner role decides customer notification with privacy/security input, including potentially captured PATs.

## Verification and reopening

Prove every old edge signature, TLS/workload credential and session protection key is rejected across replicas. Test new per-app/audience isolation, unsigned refusal, [verified-caller](../../specs/arc-verified-caller-v1.md) live membership/role enforcement, revoked grants/PATs on next use and fresh login. Verify bound-token enforcement and shared replay/nonce state under the DPoP profile, without downgrading bound tokens. Check epochs, fence-log coverage and clean image/custody before a staged reopen of all affected apps.

## Rehearsal

**rehearsal issue needed** for whole-process recovery, transit-credential inventory and multi-authority revocation. [Cratis/Infrastructure #294](https://github.com/Cratis/Infrastructure/issues/294) covers edge-key provisioning/rotation as a sub-drill only.

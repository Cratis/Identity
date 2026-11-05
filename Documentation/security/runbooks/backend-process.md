# Recovery: one backend app process

Status: Draft. Owner: product authorization and consumer maintainer with key custodian and recovery/on-call role; **woksin holds the role (Identity #20)**. Follow the [shared rules](../compromise-matrix-v1.md#recovery-rules-shared-by-the-runbooks) and [revocation §§3, 6–8](../../specs/identity-revocation-v1.md). Scope includes app data, Chronicle/workload credentials and received tokens, not just the public envelope key.

## Trigger and detection

Unexpected app execution/egress, deployment changes, data reads/writes, workload identity use, Chronicle operations or verifier tampering. Establish earliest exposure and reachable stores/secrets. In compact/self-hosted profiles, determine whether the app embeds Identity: if so, use [Identity-host recovery](shared-identity-host.md), not the detached hosted-app assumption.

## Immediate containment (ordered)

1. Isolate all affected app replicas from ingress, data stores and outbound access using trusted administration; preserve evidence.
2. Disable the app's Chronicle/workload credentials and stop background jobs or queues that can continue using them. Treat app-side authorization and logs as suspect.
3. Inventory credentials and data reachable by the process. Escalate sibling, AuthProxy, issuer or control-plane access; recover a clean image/configuration and independently verify product membership/policy state before reconnecting.

## Key and credential rotation

Revoke compromised app Chronicle credentials, workload/service identities, OAuth client secrets, runner credentials and TLS identities it could read; provision replacements with the same narrow app entitlements on the clean deployment, then verify consumers before reconnecting. No acceptance overlap for exposed credentials. Rotate a resident v1 HMAC secret via [HMAC recovery](edge-key-hmac.md). A v2 public verifier key does not itself require private-key rotation. Keep issuer/edge private keys outside the app; rotate them only if exposure is found. Record legitimate, unexposed rollover bounds separately from emergency retirement.

## Revocation actions

Fence authorizations/refresh families for the app's audience and app sessions, delegation/approval/runner records accessible during the window. Revoke exposed local PATs through the product authority. Advance the applicable trusted app/grant epoch and enforce it; broaden with owner coordination if the deployed mechanism only supports realm-wide invalidation. Use atomic fence/outbox plus durable independent log acknowledgement, not a projection update. Check for malicious memberships, grants or clients; remove unauthorized facts through their owning authorities without resurrecting old credentials. Honor offline-token denial/expiry and fail-closed restore rules before readiness.

## Audit review for the exposure window

Review independent workload/cluster, secret-store and Chronicle audit, AuthProxy sessions/access records, product data changes, membership/role/tenant changes and outbound destinations. Correlate non-secret ids and detect exfiltration, persistence, queued work or cross-app attempts. A compromised backend can bypass its own checks and falsify its logs; obtain independent evidence and investigate committed data damage separately.

## Communication

Notify the product owner, accountable issuer/security owner, AuthProxy/Arc maintainers, key custodian and on-call/privacy roles. Include affected tenants/data and exposure uncertainty. The owner role makes the customer notification decision with privacy/security input.

## Verification and reopening

Prove old app credentials cannot access Chronicle/workload endpoints, old sessions/grants/PATs deny, and new credentials have only intended app entitlements. Test sibling store/audience denial, clean verifier configuration, no unsigned fallback, fresh login and live product membership/policy under the [verified-caller contract](../../specs/arc-verified-caller-v1.md). Verify queued work, restored fences/epochs and data integrity before enabling traffic/jobs.

## Rehearsal

**rehearsal issue needed** for app isolation, credential replacement, data review and clean redeployment. [Cratis/Infrastructure #294](https://github.com/Cratis/Infrastructure/issues/294) applies only to a resident v1 edge key's rotation sub-drill, not full backend recovery.

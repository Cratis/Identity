# Recovery: shared Identity host

Status: Draft. Owner: Identity module/issuer maintainer, hosted realm lifecycle maintainer and key custodian with recovery/on-call role; **owner role pending #20**. Follow the [shared rules](../compromise-matrix-v1.md#recovery-rules-shared-by-the-runbooks) and [revocation §§3, 6–8](../../specs/identity-revocation-v1.md).

## Trigger and detection

Unexpected issuance, client registration, key/store access, realm selection, image/secret changes, or fence-log/watermark anomalies. Inventory **every realm served or reachable**, not only the reporting realm, and establish earliest possible compromise. Under **pending #17 (D19)** option B, this includes all hosted Stage realms on the shared host. Under **pending #15 (D13)** a shared products issuer exposes Direct and Studio together. See [decision acceptance](../compromise-matrix-v1.md#decision-dependencies-and-acceptance); neither acceptance has been granted by this runbook.

## Immediate containment (ordered)

1. Stop issuance, refresh and session redemption on all affected host replicas/realms; deny affected resource routes independently. Preserve external audit evidence and inventory keys, stores, clients and credentials.
2. Disable host workload/secret access and signing authority, distrust exposed issuer keys at every consumer and invalidate compromised sessions. If independent administration or fence-log custody is suspect, escalate to [cluster-level recovery](control-plane.md).
3. Recover a patched clean host with trusted realm routing, per-realm key/store boundaries and independently verified policy/account/client facts. Keep the products tier separate from Stage. A restored projection alone cannot establish trustworthy state.

## Key and credential rotation

For **every served realm**, create fresh signing keys and replace exposed encryption/session-protection keys, host TLS/workload credentials, store credentials, readable OAuth client secrets and upstream provider credentials. Provision replacement authority on clean components, publish trusted replacement JWKS/metadata to consumers, verify consumers while closed, then enable clean issuance only after revocation readiness. Revoke old access and retire compromised key ids explicitly on all verifiers/caches; **no acceptance overlap** for compromised keys. Routine JWKS cache rollover is not emergency invalidation. Follow [access-token rotation rules](../../specs/identity-access-token-v1.md) without re-enabling old keys for convenience. Rotate reachable edge keys only where actually exposed; broaden scope rather than assuming clean custody.

## Revocation actions

Revoke **every grant and session in every affected realm**: OAuth authorizations, refresh families, browser/on-behalf/runner sessions, delegation descendants and pending approvals; fence exposed product-local PATs through their authorities. Require affected users to re-authenticate. Advance a trusted monotonic realm epoch for each affected realm, emit `RealmPolicyEpochAdvanced` via the specified protocol and prove verifier-wide enforcement; an epoch claim alone cannot reject a maliciously minted token. Use atomic fence/outbox and durable independent log acknowledgements. Review/remove unauthorized accounts, clients and policy changes through authoritative writers. Do not blanket-disable legitimate accounts as a substitute for revoking compromised credentials.

If restoring, reconcile fences/tombstones/outboxes through a proven contiguous barrier for each authority/realm partition, never roll epochs back, and invalidate potentially stale refresh families/approvals unless independent consumption coverage proves safety. If coverage or trusted invalidation cannot be proven, remain denied. Reject exposed signing keys everywhere or wait the confirmed maximum token lifetime plus skew after the last possible malicious issuance while closed; refresh/session/PAT revocation still remains required.

## Audit review for the exposure window

Review external KMS/secret-store, workload/deployment, issuance, client/consent, account-linking, realm-store, fence-log/outbox and consumer authorization audit. Correlate issuer, realm, audience, tenant and non-secret credential/key/fence ids. Look across **all** served realms for forged issuance, malicious clients/links/grants, epoch rollback, deleted or missing fences, exfiltration and unauthorized product actions. Treat host-local evidence as suspect. Investigate downstream committed work and data impact separately.

## Communication

Notify accountable issuer/security owner, Identity/OpenIddict and event-contract maintainers, key custodian, hosted realm lifecycle owner, AuthProxy/Arc maintainers, every affected product/Stage realm owner and on-call/privacy roles. State shared-host versus shared-products scope explicitly. The owner role decides customer notification with privacy/security input; one realm reporting the incident does not limit who must be considered.

## Verification and reopening

For each realm and consumer replica prove old-key rejection, fresh signed-token validation, retired session/refresh/grant/approval denial and current epoch enforcement. Prove trusted issuer-to-realm selection cannot choose another realm's keys/store, foreign audiences fail and fresh authentication still requires live product membership/roles under [verified caller](../../specs/arc-verified-caller-v1.md). Validate per-partition recovery barriers, outbox reconciliation, consumption invalidation and clean custody before reopening. Recheck D13/D19 release acceptance and isolation-tier inventory; separate realm names are not separate process authority.

## Rehearsal

[Cratis/Infrastructure #299](https://github.com/Cratis/Infrastructure/issues/299): restore and signing-key rotation drills for the products realm, including D13 controls and fence-log recovery. **rehearsal issue needed** for multi-realm shared Stage-host recovery and any per-Performance variant; #299 is not evidence of that coverage. [#294](https://github.com/Cratis/Infrastructure/issues/294) applies only if edge keys were also exposed. No completed drill is claimed.

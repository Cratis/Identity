# Recovery: shared Identity host

Status: Draft. Owner: Identity module/issuer maintainer, hosted realm lifecycle maintainer and key custodian with recovery/on-call role; **woksin holds the role (Identity #20)**. Follow the [shared rules](../compromise-matrix-v1.md#recovery-rules-shared-by-the-runbooks) and [revocation §§3, 6–8](../../specs/identity-revocation-v1.md).

## Trigger and detection

Unexpected issuance, client registration, key/store access, realm selection, image/secret changes, or fence-log/watermark anomalies. Inventory **every realm served or reachable**, not only the reporting realm, and establish earliest possible compromise. Under D19 (option B, Identity #17), one platform-run Identity host serves customer Stage app realms, so this includes all hosted Stage realms on the host. Separate stores do not contain a compromise of the host itself, so this runbook's scope covers recovery across **all** realms the host runs: per-realm key rotation and rejection, invalidation of grants and sessions, and forced reauthentication. The products realm (Direct and Studio, D13, Identity #15) runs on its own host, not the shared customer host. See [decision acceptance](../compromise-matrix-v1.md#decision-dependencies-and-acceptance).

## Immediate containment (ordered)

1. Stop issuance, refresh and session redemption on all affected host replicas/realms; deny affected resource routes independently. Preserve external audit evidence and inventory keys, stores, clients and credentials.
2. Disable host workload/secret access and signing authority, distrust exposed issuer keys at every consumer and invalidate compromised sessions. If independent administration or fence-log custody is suspect, escalate to [cluster-level recovery](control-plane.md).
3. Recover a patched clean host with trusted realm routing, per-realm key/store boundaries and independently verified policy/account/client facts. Keep the products tier separate from Stage. A restored projection alone cannot establish trustworthy state.

## Key and credential rotation

For **every served realm**, create fresh signing keys and replace exposed encryption/session-protection keys, host TLS/workload credentials, store credentials, readable OAuth client secrets and upstream provider credentials. Provision replacement authority on clean components, publish trusted replacement JWKS/metadata to consumers, verify consumers while closed, then enable clean issuance only after revocation readiness. Revoke old access and retire compromised key ids explicitly on all verifiers/caches; **no acceptance overlap** for compromised keys. Routine JWKS cache rollover is not emergency invalidation. Follow [access-token rotation rules](../../specs/identity-access-token-v1.md) without re-enabling old keys for convenience. Rotate reachable edge keys only where actually exposed; broaden scope rather than assuming clean custody.

## Chronicle workload credentials

The credential the host uses to reach Chronicle must share no trust root with the people realm. It is issued outside the realm, is not derived from or signed by realm keys or OpenIddict client secrets, and lives at its own secret-store path, so rotating or losing realm secrets (an issuer-key-only exposure) neither exposes nor invalidates it, and the reverse. A compromise of the host process itself includes the credential resident on it, so rotate and revoke it as part of host recovery. Rotate it separately and revoke it at its issuer. Provisioning is infrastructure work; see the [fence-log runbook](fence-log-restore.md#chronicle-workload-credentials).

## Revocation actions

Revoke **every grant and session in every affected realm**: OAuth authorizations, refresh families, browser/on-behalf/runner sessions, delegation descendants and pending approvals; fence exposed product-local PATs through their authorities. Require affected users to re-authenticate. Advance each affected realm's recovery epoch in independent custody, emit `RealmRecoveryEpochAdvanced` and prove verifier-wide enforcement, so that every stateful credential created before it is denied ([revocation §6](../../specs/identity-revocation-v1.md)). A policy epoch only forces re-evaluation and is not enough here. An epoch claim alone cannot reject a maliciously minted token. Use atomic fence/outbox and durable independent log acknowledgements. Review/remove unauthorized accounts, clients and policy changes through authoritative writers. A compromised host can mint valid tokens for any user of every realm it serves, so treat every privileged action in the window as potentially forged in every affected realm: revoke all product-local PATs, delegation grants and approvals issued in the window, and have each affected product owner review and reverse memberships, role grants, invitations, account links and client registrations created or changed in the window. Recovery is not complete until each foothold is removed or confirmed legitimate by an accountable owner whose own membership and roles were not created or changed in the window, through an out-of-band check independent of the compromised issuer. The account a change benefits can never confirm it; a fresh sign-in proves only who signs in, not that the change was intended. Do not blanket-disable legitimate accounts as a substitute for revoking compromised credentials.

If restoring, follow [revocation §6](../../specs/identity-revocation-v1.md): advance the recovery epoch so every stateful credential, PATs included, is re-created; never roll epochs back; keep issuance denied until every restriction fact is re-established from independent evidence; and verify the key and client-secret retirement tombstones. If recovery or restriction coverage cannot be proven, remain denied. Reject exposed signing keys everywhere or wait the confirmed maximum token lifetime plus skew after the last possible malicious issuance while closed; refresh/session/PAT revocation still remains required.

## Audit review for the exposure window

Review external KMS/secret-store, workload/deployment, issuance, client/consent, account-linking, realm-store, fence-log/outbox and consumer authorization audit. Correlate issuer, realm, audience, tenant and non-secret credential/key/fence ids. Look across **all** served realms for forged issuance, malicious clients/links/grants, epoch rollback, deleted or missing fences, exfiltration and unauthorized product actions. Treat host-local evidence as suspect. Investigate downstream committed work and data impact separately.

## Communication

Notify accountable issuer/security owner, Identity/OpenIddict and event-contract maintainers, key custodian, hosted realm lifecycle owner, AuthProxy/Arc maintainers, every affected product/Stage realm owner and on-call/privacy roles. State shared-host versus shared-products scope explicitly. The owner role decides customer notification with privacy/security input; one realm reporting the incident does not limit who must be considered.

## Verification and reopening

For each realm and consumer replica prove old-key rejection, fresh signed-token validation, retired session/refresh/grant/approval denial and current epoch enforcement. Prove trusted issuer-to-realm selection cannot choose another realm's keys/store, foreign audiences fail and fresh authentication still requires live product membership/roles under [verified caller](../../specs/arc-verified-caller-v1.md). Validate per-partition recovery barriers, outbox reconciliation, consumption invalidation and clean custody before reopening. Recheck D13/D19 release acceptance and isolation-tier inventory; separate realm names are not separate process authority.

See also: [Outage behavior](../outage-behavior.md) for what keeps working while a store is down.

## Rehearsal

[Cratis/Infrastructure #299](https://github.com/Cratis/Infrastructure/issues/299): restore and signing-key rotation drills for the products realm, including D13 controls and fence-log recovery. **rehearsal issue needed** for multi-realm shared Stage-host recovery and any per-Performance variant; #299 is not evidence of that coverage. [#294](https://github.com/Cratis/Infrastructure/issues/294) applies only if edge keys were also exposed. No completed drill is claimed.

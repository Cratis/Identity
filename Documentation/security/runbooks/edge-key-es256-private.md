# Recovery: AuthProxy's v2 ES256 edge private key

Status: Draft. Owner: AuthProxy maintainer and key custodian with affected product maintainers; **woksin holds the role (Identity #20)**. Scope: every app signed by the leaked key. Follow the [shared rules](../compromise-matrix-v1.md#recovery-rules-shared-by-the-runbooks) and [revocation §§3, 6–8](../../specs/identity-revocation-v1.md). Process access escalates to [AuthProxy recovery](authproxy-process.md).

## Trigger and detection

Suspected private-key export, unauthorized KMS signing, secret-store access or forged accepted envelopes. Determine all apps/audiences using the key, replicas and signing permissions, and earliest plausible exposure. The intended per-app key boundary must be verified, not assumed.

## Immediate containment (ordered)

1. Deny affected apps' identity-bearing routes and preserve key-access, signing and verifier evidence.
2. Disable compromised signing authority and remove its key id from all affected verifiers/caches. A stolen copy may still sign, so disabling KMS alone is insufficient.
3. Close the custody/signing-access breach on clean infrastructure; widen to AuthProxy/control-plane recovery if private authority remains suspect.

## Key and credential rotation

Create distinct replacement private keys per affected app in AuthProxy-only custody (sign-only KMS where supported). Install trusted replacement public keys on clean app verifiers, configure clean AuthProxy signers, and test before reopening. Retire old keys and permissions everywhere. **No acceptance overlap** for a compromised key; record cache/replica invalidation. App public keys are not secrets requiring a separate secret rotation. Rotate credentials exposed alongside the private key; do not rotate issuer keys without evidence of issuer exposure.

## Revocation actions

For every affected app, fence sessions, exposed OAuth authorizations/refresh families, delegation lineage and approvals; revoke all product-local PATs, delegation grants and approvals issued in the exposure window at their own authorities, and have each product owner review and reverse memberships, role grants and invitations created or changed in the window (forged envelopes can impersonate existing administrators). Recovery is not complete until each foothold is removed or confirmed legitimate by an accountable product owner whose own membership and roles were not created or changed in the window, through an out-of-band check independent of the envelope path. The account a change benefits can never confirm it, since a forged envelope may have created that account or membership; a fresh sign-in proves only who signs in, not that the change was intended. Advance applicable app/grant invalidation epochs through a trusted mechanism; if a realm-wide invalidation is necessary, coordinate the wider scope explicitly. Require fence/outbox and independent log acknowledgement, and verify status/epoch enforcement before readiness. Use the shared rules' offline-token denial/expiry boundary; a new edge signature alone does not revoke old credentials.

## Audit review for the exposure window

Review KMS/secret-store and deployment audit, AuthProxy signing/session records, Arc verification/authorization, product membership/tenant operations and Chronicle writes. Search all apps signed by this key for anomalous identities, audiences, tenants, grants and data access, including correctly signed forgeries. Audit non-secret ids only.

## Communication

Notify accountable issuer/security owner, AuthProxy/Arc maintainers, key custodian, every affected product owner and on-call role. Report key reuse and scope uncertainty explicitly. Customer notification is the owner role's decision with privacy/security input.

## Verification and reopening

Across all replicas prove old signatures fail, new per-app signatures succeed only at their intended audience, sibling keys cannot cross-authenticate and unsigned fallback fails. Verify revoked sessions/grants and epochs, fresh authenticated sessions with live product authorization, and any restore fence watermarks. Recheck custody so app processes hold only public verification material. [Verified caller](../../specs/arc-verified-caller-v1.md) remains the authorization boundary.

## Rehearsal

[Cratis/Infrastructure #294](https://github.com/Cratis/Infrastructure/issues/294): rehearse private-key exposure, key-reuse scope discovery, no-overlap verifier retirement and app-session revocation. No completed drill is asserted.

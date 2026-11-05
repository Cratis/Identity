# Recovery: one app's v1 HMAC edge key

Status: Draft. Owner: AuthProxy maintainer with key custodian and product maintainer; **owner role pending #20**. Scope: key-only exposure; a compromised verifier process also requires [backend recovery](backend-process.md). Follow the [matrix's shared recovery rules](../compromise-matrix-v1.md#recovery-rules-shared-by-the-runbooks) and [revocation §§3, 6–8](../../specs/identity-revocation-v1.md).

## Trigger and detection

Suspected HMAC disclosure, unauthorized secret reads, forged envelope reports, or anomalous app/tenant identities. A valid MAC does not distinguish a forgery. Establish earliest plausible exposure and inventory every signer/verifier holding the secret; shared keys widen scope.

## Immediate containment (ordered)

1. Deny identity-bearing traffic to the affected app on every replica; preserve restricted evidence and inventory key ids before replacing workloads.
2. Remove the compromised key id from all verifiers and stop signing with it. Confirm caches/replicas reject it; never allow unsigned fallback.
3. Isolate the leak source and recover clean AuthProxy/verifier configuration. Escalate to process or control-plane recovery if key custody is not trustworthy.

## Key and credential rotation

On clean infrastructure, create a new per-app HMAC secret in the secret store, install it in the app's verifiers, then configure clean AuthProxy signers and test while routes remain denied. Revoke old secret access and remove old copies from active configuration. **No acceptance overlap** for the compromised key. Record deployment/cache completion; routine rollover overlap is not safe here. Rotate any additional credential demonstrably exposed at the leak source; the edge key alone is not the realm signing key.

## Revocation actions

Fence/revoke all affected app sessions, exposed authorizations/refresh families and related delegation/approval records through their authorities. Advance the applicable app/grant invalidation epoch and prove enforcement; if only a realm-wide safe invalidation is available, have the accountable owner approve the broader interruption rather than invent app-epoch semantics. Treat every privileged action in the exposure window as potentially forged: revoke all product-local PATs, delegation grants and approvals issued for that app in the window, and have the product owner review and reverse memberships, role grants and invitations created or changed in the window (a forged envelope can impersonate an existing administrator). Recovery is not complete until these footholds are removed or confirmed legitimate by the affected person through a fresh, verified sign-in. Check durable fence-log acknowledgement and outbox delivery. Existing bearer tokens require the shared rules' explicit denial/expiry boundary, not a claim of immediate offline revocation.

## Audit review for the exposure window

Review secret-store reads, AuthProxy signing/session logs, Arc envelope verification and authorization decisions, product membership/tenant changes and Chronicle access. Correlate app, key id, non-secret credential ids and unusual operations; review actions accepted under valid but potentially forged envelopes, not only failed MACs. Investigate data changes separately.

## Communication

Notify the accountable issuer/security owner, AuthProxy/Arc maintainers, key custodian, affected product owner and recovery/on-call role. Give scope, exposure window, denial state and revocation progress without secrets. The owner role decides customer notification with privacy/security input; this runbook does not make that decision.

## Verification and reopening

Prove the old key fails on every verifier, new envelopes work only for the intended app/audience, sibling keys are unchanged, unsigned/mismatched contexts fail, and revoked sessions/grants fail on next checked use. Prove epoch/status and any restore watermarks are current, no stale positive caches remain, and fresh login plus live product authorization succeeds before reopening. Reference [verified caller](../../specs/arc-verified-caller-v1.md) and [token limits](../../specs/identity-access-token-v1.md).

## Rehearsal

[Cratis/Infrastructure #294](https://github.com/Cratis/Infrastructure/issues/294): edge-key provisioning/rotation rehearsal. Include emergency no-overlap retirement, app-session fences and replica/cache denial evidence. No completed rehearsal is claimed.

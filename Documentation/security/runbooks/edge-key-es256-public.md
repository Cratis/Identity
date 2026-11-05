# Recovery: one app's v2 ES256 public key disclosure

Status: Draft. Owner: envelope / Arc runtime contract maintainer; **owner role pending #20**. Scope: public verification key only, not replacement of trusted verifier configuration. Follow the [shared rules](../compromise-matrix-v1.md#recovery-rules-shared-by-the-runbooks) and [revocation protocol](../../specs/identity-revocation-v1.md) if triage finds broader exposure.

## Trigger and detection

A report that the app's ES256 verification key was disclosed. Public-key possession alone permits verification, not signing. Check secret-store/signing audit and verifier configuration integrity; do not treat an unauthorized key substitution as harmless disclosure.

## Immediate containment (ordered)

1. Preserve the report and identify the exact key id, algorithm, app/audience and earliest possible exposure.
2. Confirm the material is public only and that private signing authority remains exclusively at AuthProxy/KMS; verify trusted verifier configuration is unchanged.
3. If private-key access cannot be excluded, deny affected traffic and use [private-key recovery](edge-key-es256-private.md). If app/configuration integrity is compromised, use [backend recovery](backend-process.md). Otherwise no service shutdown is required solely for public-key disclosure.

## Key and credential rotation

No emergency rotation of signing keys, TLS identities or app credentials is required for confirmed public-only disclosure. Do not move private material into the app to test it. If an unrelated routine rollover proceeds, publish the new trusted public key, switch the AuthProxy signer, retain only the bounded legitimate envelope/cache overlap, then retire the old key; record the deployed bound rather than inventing a duration. Broader compromise follows the emergency no-overlap runbook.

## Revocation actions

For confirmed public-only disclosure, no session/grant fences, PAT revocations or epoch invalidation are justified by this event alone; record that conclusion and evidence. If exposure expands, fence affected app sessions/grants, advance the applicable trusted epoch and verify durable acknowledgements under the linked protocol as part of the escalated runbook. Public disclosure is not grounds to revoke every realm user.

## Audit review for the exposure window

Review key distribution/access records, verifier configuration/deployment history, signing-key custody and unexpected signature/key-id failures. Correlate non-secret ids and look for private-key export, key replacement or forged accepted activity. Missing failures do not prove private-key custody; widen the investigation when evidence is incomplete.

## Communication

Notify the envelope/Arc maintainer, AuthProxy maintainer, key custodian and accountable issuer/security owner with the public-only finding or escalation. The owner role decides any customer notification with privacy/security input; do not label normal public-key distribution a secret leak.

## Verification and closure

Confirm a public key cannot produce an accepted signature, a trusted clean AuthProxy signature verifies, substituted/untrusted keys and unsigned envelopes fail, and the app has no private signing authority. Confirm no pending revocations were introduced by an escalated case before closing. The [verified-caller contract](../../specs/arc-verified-caller-v1.md) still requires live product authorization.

## Rehearsal

[Cratis/Infrastructure #294](https://github.com/Cratis/Infrastructure/issues/294): include public-only triage and verification-only custody alongside edge-key rotation. This is a scenario to include, not a claim of completed rehearsal coverage.

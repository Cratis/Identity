<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Identity credential record v1

Spec id: identity-credential-record
Version: 1
Status: Draft
Issue: https://github.com/Cratis/Identity/issues/129
Sign-off issue: https://github.com/Cratis/Identity/issues/14
Supersedes: None

## 1. Scope and status

This Draft defines the credential metadata that AuthProxy adapters consume before constructing a verified caller or a [forwarded identity v2](arc-forwarded-identity-v2.md) envelope. It supports [AuthProxy #294](https://github.com/Cratis/AuthProxy/issues/294) (PAT, bearer and DPoP adapters) and [AuthProxy #257](https://github.com/Cratis/AuthProxy/issues/257) (credential dispatch). RFC 2119 keywords describe the proposed contract, not completed delivery or sign-off. Existing signed-off specifications remain authoritative; this Draft does not amend them by itself.

The record identifies a verified credential, not a person, role or permission. The product still resolves the verified `(issuer, subject)` account key, tenant, live membership and product permissions. Display names, login names, email, PAT labels and request-selected tenant labels MUST NOT establish identity or authority. [Verified caller v1](arc-verified-caller-v1.md), [PAT format v1](arc-pat-format-v1.md), [delegation v1](arc-delegation-v1.md), [access-token v1](identity-access-token-v1.md), [DPoP v1](identity-dpop-v1.md) and [revocation v1](identity-revocation-v1.md) retain their respective validation and authorization rules.

## 2. Credential-record identity

`credentialId` MUST be the non-secret identifier of the verified credential record. For an Identity OAuth grant it is the existing grant event-source UUID, rendered in UUID `D` form. It is scoped to the originating grant lineage and MUST remain stable through refresh rotation and continuation of that grant. It MUST NOT be derived from an access-token `jti`, proof `jti`, client id, token bytes or request headers, body or query.

For OAuth, authenticated, authorized Identity introspection is the sole authoritative source of `credential_id` and `credential_kind`. A locally validated JWT alone MUST NOT supply the credential-record id for these envelope routes. An optional access-token `cid` is advisory: when present, it MUST equal introspection's `credential_id`; a mismatch MUST cause refusal. Absence of `cid` does not prevent acceptance when introspection supplies the required record. `cid` MUST NOT appear as an ID-token credential identifier.

| Credential | Authoritative field and owner | Verified-caller mapping |
| --- | --- | --- |
| OAuth grant | Identity introspection `credential_id`; optional JWT `cid` is only a consistency check. | `credentialKind=oauth-grant`, `credentialId=credential_id`. |
| PAT | `credentialId` in the protected product-owned PAT creation record, obtained only after local PAT verification. | `credentialKind=pat`, `credentialId` from that record. Identity OAuth introspection is not a PAT authority. |
| Machine principal or workload | `credentialId` in the credential record selected by the allowlisted workload/service verifier and trusted installation mapping. | `credentialKind=machine-principal` or `workload`; no person or human assurance is fabricated. A common authority API/id format remains outstanding. |
| Browser or on-behalf session | `credentialId` from the verified server-side session/credential record. | `credentialKind=browser-session` or `on-behalf-session`; the session authority's mapping remains subject to review. |

Once accepted, this Draft closes the OAuth-kind enum serialization that [verified caller v1](arc-verified-caller-v1.md) leaves open: the wire value is the exact string in section 3.

The forwarded v2 field is `x-cratis-credential-id`; signing it does not create authority for its value. A delegation or per-work `grantId` is a separate checked reference, not an alias for `credentialId`. The OAuth credential UUID's use of the grant event-source mapping MUST NOT make `x-cratis-grant-id`, token `jti`, or a root/parent grant reference interchangeable with it. Child/parent/root grant bounds still require their authoritative records.

**Current implementation.** `CredentialId.For` uses the consent/revocation outbox's existing mapping from the validated operational authorization id to the grant UUID. `IntrospectionCredentialClaims` emits the OAuth pair only for an access token with that authorization. Refresh preserves it. Tokens without an authorization, including the current token-exchange path, receive neither the pair nor `cid`. No fallback id is synthesized. Extending stable credential identity to other grant-lineage operations is a gap, not a claim that current token exchange or delegated issuance supplies this record. The [v1.1 amendment](identity-contracts-v1.1-amendment.md) remains Draft and records the current absence of delegated `act` issuance.

## 3. Credential kinds

The exact, case-sensitive strings are the forwarded identity v2 vocabulary:

| `credentialKind` / `credential_kind` | Meaning |
| --- | --- |
| `browser-session` | Verified browser session. |
| `oauth-grant` | Verified OAuth access token backed by an Identity grant. Bearer and DPoP are not separate credential kinds. |
| `pat` | Product-verified personal access token. |
| `on-behalf-session` | Verified on-behalf session with the separately checked grant and actor context. |
| `machine-principal` | Allowlisted verified machine principal. |
| `workload` | Allowlisted verified workload credential. |

Missing, empty, unknown or differently cased kinds MUST be refused on envelope routes before caller construction or signing. A header's kind MUST NOT select a different authority after verification fails. Identity currently emits only `credential_kind=oauth-grant`; this enum does not announce issuance or introspection support for the other kinds.

## 4. OAuth introspection response

Only an authenticated, authorized introspection client for the validated token's audience receives active credential metadata. Request parameters MUST NOT supply any returned credential id, kind, audience, tenant, scope or confirmation thumbprint.

| Member | Type and contract for an active OAuth grant |
| --- | --- |
| `active` | Boolean `true`; necessary but not sufficient for envelope construction. |
| `credential_kind` | String `oauth-grant`. |
| `credential_id` | Non-secret grant UUID from section 2. |
| `aud` | Exactly one resource-catalog audience string, compared with trusted route configuration. |
| `tid` | Verified product tenant string, required for tenant-bound/workspace routes. No `tenant_id` alias. |
| `scope` | Space-delimited string of verified scope ceilings; empty scopes do not authorize unrestricted access. |
| `cnf` | Absent for an unbound token; for a bound token, an object containing expected `jkt`, the canonical base64url SHA-256 JWK thumbprint. Not a validated proof result. |
| `cache_epoch` | Current nonnegative integer recovery epoch, used for cache invalidation, not product authorization. |

Other validated RFC 7662 metadata, including subject, issuer, client and expiry, retains its meaning. Required account and assurance evidence for a person caller still follows verified caller v1 and forwarded identity v2; it MUST NOT be manufactured from token issue time or current time.

A synthetic active, unbound response excerpt illustrates the members defined here; it is not a complete caller or a live credential:

```json
{
  "active": true,
  "credential_kind": "oauth-grant",
  "credential_id": "1ad68110-7589-4c76-9ab9-4a0553b1ed85",
  "aud": "studio-mcp",
  "tid": "tenant-fixture",
  "scope": "studio:read",
  "cache_epoch": 7
}
```

Invalid, expired or revoked tokens MUST return exactly `{ "active": false }`, with no credential metadata, confirmation or epoch. Introspection client authentication/authorization errors and recovery failures MUST NOT disclose credential metadata and MUST NOT be treated as an active record. They need not be represented as successful inactive responses.

An active legacy token without an operational authorization can lack `credential_id` and `credential_kind` in today's implementation. It MUST NOT produce an OAuth envelope under this Draft. Likewise, a missing required `tid`, audience, scope or epoch MUST NOT be repaired from a request or cached caller. There is no grace window on envelope/MCP routes for tokens without a credential id. Any legacy acceptance outside envelope routes is a separate policy, not a fallback defined here.

**Current implementation.** `IntrospectionCredentialClaims` adds `credential_id`, `credential_kind`, token `tid`, scopes and `cache_epoch` after OpenIddict validation and checks the independently observed recovery epoch before and after attachment. Built-in metadata supplies audience and other token metadata. A well-formed token `cnf` passes through OpenIddict; malformed confirmation fails before the credential metadata handler. Identity does not currently issue DPoP-bound tokens. The active-shape requirements above do not imply that this handler itself enforces every required field or compares advisory `cid`; consumer enforcement remains a gap to verify in the adapters. See [resource token validation](../security/resource-token-validation.md).

## 4a. Gateway introspection client registration

The gateway (AuthProxy) authenticates to `connect/introspect` as an introspection client declared in Identity configuration under `Identity:IntrospectionClients`. Each entry has two members, `ClientId` and `ClientSecret` (`IntrospectionClientOptions`). Identity registers each entry as a confidential client whose only permission is the introspection endpoint (`ClientDescriptors`); it cannot request tokens or use any other endpoint.

```json
{
  "Identity": {
    "IntrospectionClients": [
      { "ClientId": "authproxy", "ClientSecret": "<from secret store>" }
    ]
  }
}
```

- The value above is a placeholder. The secret MUST come from deployment secret configuration and MUST NOT be committed; the shipped `appsettings.json` files keep `"IntrospectionClients": []`, so no gateway can introspect until a deployment adds an entry.
- Active credential metadata is attached only when the introspecting client id is in the validated token's audience (`IntrospectionCredentialClaims`, specified by `when_the_caller_is_not_in_the_token_audience`). The gateway's `ClientId` therefore MUST equal the audience/resource identifier of every route it introspects for. A caller outside the audience receives no `credential_id`, `credential_kind`, `tid` or `cache_epoch`, which the gateway MUST treat as a refusal under section 3.
- Register one client per gateway deployment. Rotate the secret by replacing the entry's `ClientSecret` in deployment configuration.

## 5. Sender-binding result and ownership

The adapter MUST produce exactly one verified logical result:

- **Bound:** `bound { jkt, proof id }`, where `jkt` equals the authoritative expected `cnf.jkt` and proof id is the accepted DPoP proof's `jti`, not the access-token `jti`.
- **Unbound:** `unbound`, explicitly established from a verified credential with no required binding and permitted by current policy.

Missing or unknown results MUST cause refusal; they MUST NOT mean unbound. Malformed/unsupported confirmation MUST NOT be treated as absent. A token with `cnf.jkt` MUST NOT be accepted as plain Bearer or emit an unbound result. Unbound OAuth tokens MUST NOT acquire binding merely because a request supplies a proof or thumbprint.

DPoP proof validation stays at AuthProxy, the gateway. Identity supplies only the expected token binding; introspection does not validate the resource request's proof. AuthProxy MUST enforce [DPoP v1](identity-dpop-v1.md): scheme, signature and allowed key, external target and method, proof time, token hash, matching thumbprint, nonce and shared replay checks. Missing/invalid required proof, unavailable required state or replay MUST fail closed, with no signed request and no fallback.

These are logical result names, not new header encodings. Forwarded identity v2 encodes `unbound` as the exact `x-cratis-sender-binding` value `none`, and bound as standard-base64 UTF-8 JSON containing only `jkt` and accepted proof `jti`. Raw proofs MUST NOT be forwarded as evidence. The signature binds a result already verified at the edge; it does not validate a proof itself.

## 6. PAT authority and envelopes

PATs are product-verified today and remain unwrapped on the current path. The product that creates and verifies the protected PAT record owns its non-secret `credentialId`. AuthProxy MUST NOT invent one from the token format, checksum, product prefix, label or a header, and Identity's OAuth introspection MUST NOT be used to authenticate a PAT.

PATs MUST be scoped to the product/resource and tenant, expire, and be revocable. [PAT format v1](arc-pat-format-v1.md) supplies format, local verification, maximum 90-day expiry (or shorter policy cap), scope ceiling, creation assurance, recovery-epoch and policy rules. Revoked, expired, wrong-product or wrong-tenant PATs MUST be refused. The credential id never substitutes for the secret verifier or the record's live state.

AuthProxy MUST NOT attach a person envelope (a kind other than `pat`) to a forwarded PAT. A `pat` envelope is permitted by forwarded identity v2 only with authoritative verified PAT metadata and the exact bearer-binding rules of that specification. Even then, the receiving product's local PAT verifier MUST build the caller, enforce revocation, scope ceiling and applicable policy/recovery epochs, and compare the signed account key with its verified record. A signed `pat` kind alone never authenticates a PAT. The current unwrapped path does not provide AuthProxy with that authority, so this Draft does not enable PAT envelopes.

A future product PAT-authority verification endpoint would need to authenticate and authorize its caller and return verified active/inactive state, `credentialKind=pat`, `credentialId`, account/creation assurance, product/audience, tenant, scope ceiling, expiry, sender-binding expectation and applicable recovery/policy epoch evidence. Inactive state would disclose no credential metadata. Its transport, field serialization, caller trust, freshness and any permission to forward an envelope require review; no such endpoint is claimed to exist.

For MCP, the no-token-passthrough requirement in section 9 also applies. The current unwrapped forwarding path and v2's bearer-covering capability do not by themselves satisfy it. The product-local PAT verification boundary for MCP remains an outstanding adapter decision; a route MUST fail closed if it cannot satisfy both requirements, not silently forward a credential or construct an unverified envelope.

This Draft does not enable the PAT envelope, so the scope of [AuthProxy #294](https://github.com/Cratis/AuthProxy/issues/294) is the bearer and DPoP adapters first; PAT stays unwrapped and product-verified until the PAT-authority endpoint decision (open question 3).

## 7. Revocation freshness and failure behavior

AuthProxy MUST use a positive introspection cache of at most **30 seconds**, configurable downward, and MUST NOT cache negative results. Reuse MUST stop at token expiry or an earlier required authority deadline; a cache hit MUST NOT restart the 30-second window. Cache time is the revocation window, not extra clock skew or an extension of token lifetime.

The positive-cache key MUST be a digest of the exact presented access token plus the expected audience/resource; it MUST NOT be keyed by `credentialId`, `jti` or subject. This follows [verified caller v1](arc-verified-caller-v1.md), which requires cache keys to bind the token and expected audience.

Cached metadata MUST remain bound to the same verified credential and trusted issuer, resource/audience, tenant and validation context. A request MUST NOT select an unrelated cached caller. DPoP proof acceptance is request-specific and MUST NOT be reused from an introspection-cache hit.

Consumers MUST discard cached verification results on a recovery `cache_epoch` change. A recovery epoch MUST be checked against independently current authority state; a value from the token, old response or restored projection alone does not establish freshness. Missing, older or unavailable required recovery evidence MUST cause refusal. A policy epoch is separate: stale policy requires current-policy re-evaluation where that contract permits it, whereas stale recovery evidence MUST NOT be repaired by policy re-evaluation.

Introspection unavailability, malformed required metadata, uncertain recovery/fence state or failed validation MUST fail closed on these credential-envelope/MCP routes. A failed refresh of verification state MUST NOT serve an expired cached positive result or use local JWT validation as a substitute. Revocation can remain unseen only within the permitted positive-cache window; this is not a claim of instantaneous fleet-wide revocation. The independently current epoch distribution and edge cache implementation remain integration gaps, as recorded in resource token validation and the v1.1 amendment.

## 8. Explicit credential dispatch

AuthProxy #257 selects the credential path; #294 adapts only the successfully verified result. Selection MUST occur once, before caller construction:

1. Apply the verified caller credential gate. Multiple independent credentials, ambiguous/duplicate Authorization or a credential in the URI query MUST be refused. A DPoP proof with its token is not another credential. A verified v2 envelope with an exactly covered bearer counts as one only under that specification; an unrelated envelope and bearer count as two.
2. Recognize the PAT namespace before OAuth verification: a credential value beginning with `<key>_pat_`, matched ASCII case-insensitively by `^[a-z0-9-]+_pat_`, selects only the PAT path. Recognition is not successful PAT verification. Wrong product, malformed format, unsupported version or failed PAT verification MUST NOT reach OAuth introspection. A PAT-namespace value under a non-Bearer scheme, including DPoP, MUST be refused in that branch.
3. Select eligible non-PAT Bearer/DPoP values only for the explicitly configured OAuth verifier. Failed issuer, signature, type, time, audience, introspection, required metadata or DPoP validation MUST NOT fall back to PAT, session, workload or unsigned headers. A malformed non-PAT value remains an OAuth failure, not a guessed alternative credential.
4. Session, on-behalf and workload routes MUST use their explicitly configured verified authority. Missing/unknown kind or sender-binding result, unavailable authority or conflicting evidence MUST refuse the request, not select another adapter.
5. Only verified metadata may populate the v2 fields. Incoming identity headers MUST be stripped as required by v2; signed metadata MUST match the selected credential's id, kind, account key and sender binding. Tampered or mismatching evidence MUST cause refusal with no caller or alternate verifier.

Public failures retain the applicable generic authentication/OAuth challenge behavior from the existing specifications; the cases below do not introduce new public error strings.

## 9. MCP access-token and resource boundary

MCP OAuth endpoints MUST accept **access tokens only, never OIDC ID tokens**. They MUST independently validate trusted issuer, signature/type, expiry, audience and required scopes; `active=true` alone does not establish those checks. PAT remains the separately dispatched interim credential, not an ID token or OAuth access token.

Studio MCP and Direct MCP MUST use distinct catalog audiences, `studio-mcp` and `direct-mcp`. A client MUST request the corresponding registered canonical HTTPS resource URI using RFC 8707 `resource`; the trusted catalog maps that URI one-to-one to its audience slug. A resource URI is not the audience string. Unknown resources, another product's audience, audience arrays or missing required tenant constraints MUST be refused. One login does not authorize token reuse across products: each resource needs its own access token and scope/tenant constraints. Exact resource URIs remain a catalog decision, not values derived from Host, headers or the presented token. **Current gap:** the signed-off [scope catalog v1](arc-scope-catalog-v1.md) names `studio-api` and `direct-api` for its MCP entries, and the reference host's Direct MCP entry uses `direct-api`. The proposed `*-mcp` audiences require a reviewed catalog/registration update; this Draft neither renames existing audiences nor makes those registrations current.

MCP MUST NOT pass incoming tokens through as upstream or downstream credentials. Existing v2 support for a cryptographically covered forwarded bearer is not permission to enable MCP token passthrough. Forwarding the covered bearer to the product behind AuthProxy whose audience it carries is not token passthrough; passthrough means reusing the token at another resource or upstream. Adapters must meet the reviewed product boundary without reusing an incoming token at another resource; this Draft does not invent token exchange or a replacement issuance path. RFC 9728 protected-resource metadata and `WWW-Authenticate` discovery challenges belong to that boundary's delivery.

A local issuer MUST have a distinct fixed issuer and distinct signing keys and be configuration-gated to Development. Deployed AuthProxy/Arc MUST pin production trust and reject local-issuer tokens, with no runtime trust expansion. Neither successful local introspection nor a signed envelope may widen that trust. These boundary requirements do not claim that the per-product registrations or adapters are delivered by Identity's current metadata handler.

## 10. Conformance cases

Machine-readable data vectors for these cases live in `Source/Identity.Specs/Vectors/identity-credential-record-v1/`. The Identity specs check only their shape; they do not replay them against a gateway.

These cases describe required outcomes, not an implemented or passing conformance suite. All other required checks are assumed to pass in an acceptance case.

| Case | Expected result |
| --- | --- |
| Active OAuth grant, matching issuer/audience/tenant/scopes, explicit permitted unbound result | Construct one caller from verified evidence; emit matching credential metadata. |
| Refresh rotation of that grant | `credential_id` stays equal; token `jti` can change; present `cid` remains equal to the id. |
| Advisory `cid` differs from introspection id | Refuse; never prefer the claim. |
| Revoked/expired/invalid token | Exactly `active:false`, no metadata; no caller or negative cache. |
| Legacy active token without grant id/kind; missing required tenant, scope or epoch | Refuse envelope construction; no synthesized id or request-derived replacement. |
| Bound token, valid matching request proof | Bound result contains expected `jkt` and accepted proof `jti`; no raw proof forwarded. |
| Bound token without proof, or presented as Bearer | Refuse; never downgrade to unbound. |
| Bound token with wrong key, method, target, hash, time, nonce or replayed proof | Refuse; no envelope or alternate verifier. |
| Unbound Bearer, no proof, policy permits | Explicit unbound result (`none` on v2 wire). |
| Unbound token with DPoP scheme/proof, malformed confirmation, or policy requiring binding | Refuse; proof cannot create issuer binding. |
| Missing/unknown kind or sender-binding result | Refuse before signing/caller construction. |
| Valid product/tenant PAT on supported product-local path | Local PAT verifier owns caller and id; no invented person envelope. |
| Revoked/expired/wrong-product/wrong-tenant PAT, malformed PAT namespace, or PAT with DPoP scheme | PAT branch refuses; no OAuth verification/introspection. |
| Failed OAuth verification/introspection | Refuse; PAT, session and workload verifiers are not attempted. |
| Signed non-`pat` envelope with PAT, or `pat` envelope with revoked PAT | Refuse; signature does not replace local verification. |
| Envelope/bearer hash mismatch, signed account/id/kind/binding mismatch or changed covered metadata | Refuse; no caller and no alternate verifier. |
| Multiple independent credentials, repeated Authorization or query credential | Refuse before dispatch. |
| Two tokens for the same credential, subject or `jti`, or one token presented for two audiences/resources | Separate cache entries; the key is the token digest plus expected audience/resource, never `credentialId`, `jti` or subject. |
| Positive cache reaches 30 seconds, token expiry, or earlier authority deadline | Require fresh verification; never extend or serve stale positive state. |
| Recovery epoch changes, is older/missing, or independent current state is unavailable | Invalidate affected cache; refuse unless current required evidence is established. |
| Introspection or required shared proof/recovery state unavailable | Fail closed, with no local/alternate fallback. |
| ID token at MCP; Studio token at Direct MCP; unknown resource or audience array | Refuse, independently of a valid signature or introspection activity. |
| Deployed configuration receives a local-issuer token | Refuse despite locally valid signing/introspection. |
| MCP route can operate only by passing the incoming token upstream/downstream | Refuse; do not enable passthrough as an adapter fallback. |
| Request with conflicting tenant selection (token tenant versus requested tenant/namespace) | Refuse; user mapping binds the verified `(issuer, subject)` or immutable upstream keys, never login or email. |

## 11. Open questions (outstanding decisions)

Review by woksin and the Identity/AuthProxy owners (including L101) is required. None of the following is sign-off:

1. Confirm the OAuth id/lineage wording and future child/exchange mapping. Current no-authorization tokens have no record pair; do not substitute `jti`, client id or a root/parent grant reference. Confirm any consumer enforcement still needed for advisory `cid` equality and required active metadata.
2. Confirm session, machine-principal and workload credential-record authority, id serialization and adapter field mappings. The v2 kind strings are already specified; issuance and common verification APIs for those records are not delivered here.
3. Decide the product PAT-authority verification endpoint and whether/when a PAT envelope may be emitted. Reconcile product-local PAT verification and today's unwrapped path with the MCP no-token-passthrough boundary before enabling that path; a v2 signature alone is insufficient.
4. Confirm independent current recovery-epoch distribution, positive-cache invalidation and fail-closed edge integration. Identity emits `cache_epoch`; this document does not establish a completed consumer implementation or a new revocation protocol.
5. Confirm DPoP issuance and gateway adapter delivery and the unchanged v2 result mapping. Current confirmation pass-through is not proof validation or bound-token issuance.
6. Register the canonical resource URIs and scope/catalog mappings for `studio-mcp` and `direct-mcp`, deliver metadata/challenges, and verify deployed rejection of the Development-only issuer. The exact URIs and the no-passthrough adapter design are not supplied by this Draft.
7. Machine-readable Draft vectors now exist under `Source/Identity.Specs/Vectors/identity-credential-record-v1/` (active, revoked, legacy, bound with valid proof, bound without proof, unbound with proof, plus `cid` mismatch, unknown kind and missing sender-binding result). They are data only, not runtime evidence: the bound cases describe gateway behavior for a hypothetical bound token because Identity issues none yet. Executable replay is still to be assigned to AuthProxy [#294](https://github.com/Cratis/AuthProxy/issues/294) and [#257](https://github.com/Cratis/AuthProxy/issues/257) and to products. The prose cases above are not runtime evidence either.
8. Record required reviews and maintainer/owner sign-off in [Identity #14](https://github.com/Cratis/Identity/issues/14) before changing Draft status. This document supersedes no signed-off contract.

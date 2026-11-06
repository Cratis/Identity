# DPoP edge profile

```text
Spec id: identity-dpop
Version: 1
Status: Signed off
Signed off: 2026-11-05 by woksin, https://github.com/Cratis/Identity/issues/14; owner confirmation pending
Issue: https://github.com/Cratis/Identity/issues/29
Sign-off issue: https://github.com/Cratis/Identity/issues/14
Supersedes: None
```

## 1. Scope and authority

Keywords follow [the specification conventions](README.md). The [Terms of identity-contract-v1](identity-contract-v1.md#terms) apply **verbatim**. This specification defines Identity's issuance and AuthProxy's enforcing edge for RFC 9449 DPoP. Arc consumes the verified result through signed envelope v2; it does not re-validate the incoming proof on that path. Native OAuth verifiers MUST implement the same proof requirements themselves, without a fallback verifier after failure.

This is a signed-off contract (Identity #14, 2026-11-05), not deployed behavior. DPoP is sender constraint, not client authentication, body integrity, live membership or permission. PATs, browser cookies and workload service-account bearer credentials are not made DPoP credentials by this profile. Runners' registered-key session protocol is separate; their OAuth tokens follow this profile.

## 2. Client selection and issuance

| Client or policy | Issuer requirement |
|---|---|
| Cratis CLI, Cratis agents and runners using OAuth | Registration MUST require DPoP via `dpop_bound_access_tokens=true`. |
| Delegated-actor token exchange (RFC 8693) | Exchanged tokens MUST be DPoP-bound to the actor's key proven at the token endpoint. Exchange without a valid DPoP proof MUST be refused; Bearer issuance is forbidden. |
| Confidential organisation-automation clients using `client_credentials` | Registration MUST set `dpop_bound_access_tokens=true`; clients MUST authenticate with `private_key_jwt` and prove the DPoP key at the token endpoint. Unbound issuance MUST be refused. |
| MCP client with verified DPoP capability, opting in | Registration MUST set `dpop_bound_access_tokens=true`; subsequent issuance MUST NOT silently downgrade. |
| MCP client without established DPoP support | DPoP MUST remain optional unless organisation policy requires it. An unbound grant MAY issue Bearer tokens only when policy permits. |
| Any client in an organisation requiring sender-constrained tokens | Identity MUST require DPoP regardless of a false/absent registration flag. A client unable to supply a valid proof MUST receive no access or refresh token. |

Identity MUST resolve registration metadata from the trusted client record and organisation policy from the confirmed organisation binding, never a request-supplied policy or organisation label. A public client id is not authenticated actor evidence. A valid voluntary proof on an otherwise optional issuance MUST produce a bound token, not an ignored proof and Bearer token. An invalid supplied proof MUST fail rather than fall back to unbound issuance. Tightening policy MUST be enforced on refresh and exchange as well as initial issuance; an existing unbound refresh grant MUST NOT bypass a new DPoP requirement.

For bound issuance:

1. Identity MUST require exactly one `DPoP` header at the token endpoint and validate the proof as in §3, with `htm=POST` and `htu` equal to the configured public token endpoint. Token-endpoint proofs do not require `ath` (there is no presented access token). Identity MUST require its own AS nonce (§5), independently of resource-server nonces.
2. Identity MUST compute the RFC 7638 SHA-256 thumbprint of the verified proof's public JWK and emit `cnf: {"jkt": "<thumbprint>"}` in the access token, with token response `token_type="DPoP"`. [Access-token v1 §2.2 and §5](identity-access-token-v1.md) retain their claim allowlist and validation order. `cnf` is already conditionally allowed by [contract v1 §5](identity-contract-v1.md#5-token-claims); no new claim is added here.
3. Identity MUST bind the refresh token and every rotated replacement to the same key thumbprint in authoritative server-side state, including for public clients (RFC 9449 §5). Redemption MUST prove that key at the token endpoint; a missing/wrong proof MUST NOT mint tokens or change the binding. Key replacement requires a separately authorized new grant, not a caller-selected refresh-key swap.
4. The [revocation profile's refresh-family rotation, reuse and fences](identity-revocation-v1.md#4-redemption-and-concurrency) remain mandatory. DPoP MUST NOT replace those checks or widen resource, tenant, scope, lifetime or authentication evidence. A DPoP-required registration MUST never receive an unbound token, even if an issuance handler would otherwise omit `cnf`.

AS metadata MUST advertise `dpop_signing_alg_values_supported=["ES256"]`. Invalid/missing required proofs at issuance MUST return OAuth HTTP 400 `invalid_dpop_proof`, with no tokens. A missing, stale or invalid AS nonce on an otherwise valid proof MUST instead return HTTP 400 `use_dpop_nonce` and a fresh `DPoP-Nonce`. Identity MUST apply the same proof format, algorithm, key, signature, method/URI, `iat`/`jti` and shared-cache checks as §3, substituting its configured token endpoint and AS nonce domain, omitting resource `ath` validation, and checking the existing refresh-key binding when redeeming a bound refresh token. A client without DPoP support blocked by organisation policy MUST receive `invalid_dpop_proof` with `error_description="Organisation policy requires DPoP; use a DPoP-capable client."`; it MUST NOT be redirected into an unbound grant. A UI detecting this incompatibility SHOULD explain it before authorization.

## 3. AuthProxy validation order

Machine routes MUST accept a bound access token only as `Authorization: DPoP <token>` accompanied by exactly one `DPoP: <compact-proof-JWS>` header. Duplicate/ambiguous credentials or proof headers MUST be refused before dispatch; a proof accompanying its token is not a second access credential ([MCP v1 §2](arc-mcp-authorization-v1.md)). No failed DPoP check permits Bearer, PAT or session fallback.

AuthProxy MUST validate in this order. An eligible unbound Bearer request without a proof skips steps 3–13, reaches the current-policy check at step 14, and can produce only an explicit unbound result. Reasons are vector/diagnostic vocabulary, not additional public OAuth error values.

| Step | Required check | Failure reason |
|---|---|---|
| 1 | Validate the access token under access-token v1 §5 steps 1–12, including trusted issuer, signature, time, audience and claim allowlist. | Access-token profile reason |
| 2 | If token has `cnf.jkt`, require DPoP scheme and proof. A bound token sent as Bearer MUST fail even if a proof is supplied. An unbound token with DPoP scheme MUST fail: a proof cannot retrofit issuer binding. Unbound Bearer is allowed only when policy permits, and an accompanying DPoP header MUST be refused rather than treated as binding. | `bound_token_as_bearer`, `missing_proof`, `unbound_token_as_dpop`, `unexpected_proof` |
| 3 | Require a three-part compact JWS with header/payload JSON objects, no duplicate member names and no unsupported critical headers; `typ` MUST be exactly `dpop+jwt`. | `malformed_proof`, `invalid_typ` |
| 4 | Allow exactly `alg=ES256`. Reject `none`, `HS256`, every symmetric algorithm and every other algorithm. | `invalid_alg` |
| 5 | Require an embedded public `jwk` with `kty=EC`, `crv=P-256`, valid public point and ES256-compatible use/key operations if present. Reject private material (including `d`) and symmetric keys; do not fetch `jku`/`x5u` or use token-supplied key URLs. | `invalid_jwk` |
| 6 | Verify the proof signature using only that public key and ES256. | `invalid_signature` |
| 7 | Require nonempty string `htm` exactly equal, case-sensitively, to the external HTTP method; do not uppercase/repair it. | `invalid_htm` |
| 8 | Require string `htu` exactly equal to the configured public HTTPS origin plus the external escaped path (including any configured mount prefix), with no query or fragment. A proof `htu` containing either MUST fail. The request's query is excluded. Never construct the expected URI from internal routing, `Host`, `Forwarded` or `X-Forwarded-*`. | `invalid_htu` |
| 9 | Require integer `iat` with `now - 300 < iat <= now + 30`, and nonempty unpredictable string `jti` (clients MUST generate at least 128 random bits per proof). | `invalid_iat`, `invalid_jti` |
| 10 | Require `ath` equal to unpadded base64url SHA-256 of the ASCII bytes of the exact presented access token, without the authorization scheme or whitespace. | `invalid_ath` |
| 11 | Require `cnf.jkt` equal to the RFC 7638 SHA-256 thumbprint of the proof JWK; do not hash the entire JWK serialization or accept a caller-supplied thumbprint. | `jkt_mismatch` |
| 12 | Require a valid resource-server nonce under §5. A missing, expired or invalid nonce MUST challenge, not forward. | `use_dpop_nonce` |
| 13 | Atomically reserve the previously unseen proof id in the fleet-wide shared replay cache (§4). A duplicate or unavailable/uncertain cache MUST fail closed. | `replayed_jti`, `jti_cache_unavailable` |
| 14 | Apply current required-binding policy and other credential/target checks before forwarding a signed verified result. An unbound token MUST fail if policy now requires binding. | `dpop_required` |

Public-path mapping MUST be deterministic trusted route configuration shared by replicas, including path rewrites; a route without a trustworthy public mapping MUST fail closed. Normalization beyond that mapping MUST NOT make an internal host or a different escaped path match.

## 4. Fleet replay and restart behavior

AuthProxy replicas MUST use one authoritative shared cache per deployment security domain, not per-replica memory. Identity MUST likewise share its token-endpoint replay cache across AS replicas. Cache keys MUST scope `(validation domain, public origin, jkt, proof jti)`; a resource request MUST NOT key by replica, method, path or token id, since that would permit reuse of one proof id elsewhere in the domain. AS and resource domains MUST be distinct.

Reservation MUST be atomic insert-if-absent with TTL **390 seconds** from acceptance. The retention formula is `maximum proof age + future-iat allowance + maximum inter-validator clock difference = 300 + 30 + (2 × 30) = 390 seconds`. Each validator MUST remain within ±30 seconds of the fleet time source aligned to UTC, so any two validator clocks can differ by at most 60 seconds. Validators MUST refuse service when their clock offset exceeds that bound or cannot be established. This covers the last possible acceptance of even a future-dated proof by a slower replica. The nonce lifetime in §5 is an additional acceptance constraint, not a replacement for proof-age validation; it MUST NOT shorten this retention. Both checks apply, so their lifetimes do not add serially. Nonce validation precedes insertion so that an unsuccessful nonce challenge does not consume the proof id; clients MUST nevertheless sign a new proof for every retry. Concurrent identical proofs MUST have at most one winner. Access-token `jti` is not a proof id and MUST NOT be single-use cached as a substitute.

Restarts, failover and replica changes MUST preserve reservations until TTL expiry. No early eviction is allowed. An unavailable cache, ambiguous insert result or loss of reservation coverage MUST deny forwarding/issuance; after cache data loss, serving MUST wait at least 390 seconds (the same retention formula above) after the last possible pre-loss acceptance, with healthy shared state, before resuming. A process-local fallback is forbidden.

## 5. Nonces and challenges

This profile chooses **stateless HMAC-SHA-256 nonces with fleet-shared keys**, avoiding per-nonce storage while preserving replica/restart validity. Nonce keys MUST be distinct from access-token signing keys and envelope keys, stored in the secret store, and shared only by validators in the same nonce domain. AS and resource-server nonce domains MUST use separate keys.

A nonce MUST authenticate a version, key id, validation domain, configured public origin, `jkt`, integer issuance time and at least 128 random bits, using an unambiguous encoding. Validators MUST verify the MAC in constant time and match domain/origin/key binding, require `issued_at <= now + 30` and `now < issued_at + 300 + 30`. No nonce secret or real proof belongs in logs/events. Nonces MAY be reused within that interval with distinct proofs; the shared `jti` cache, not the nonce, enforces single use.

Identity and AuthProxy MUST issue a fresh `DPoP-Nonce` on nonce challenges and SHOULD rotate it on successful DPoP responses. Clients MUST keep AS and resource nonces separately and sign the most recently received nonce into the next proof. Validators MUST accept still-valid older nonces to support concurrent requests. Key rotation MUST retain verification keys until all issued nonces expire; replicas missing a required key MUST fail closed. Clock monitoring MUST enforce §4's ±30-second offset bound against the fleet time source and refuse service outside it. The nonce encoding and key distribution are defined in §5.1; they are not part of the envelope contract, whose own random nonce is unrelated.

### 5.1 Nonce encoding and keys

Decided in Identity #14.

- **Encoding.** A nonce is the unpadded base64url form of 66 bytes (88 characters), within RFC 9449's nonce grammar: `ver` (1 byte, `0x01`); `domain` (1 byte: `0x01` for authorization-server nonces issued by Identity, `0x02` for resource nonces issued by AuthProxy); `kid` (8 opaque bytes); `issued_at` (8 bytes, unsigned big-endian Unix seconds); `random` (16 bytes from a CSPRNG); `mac` (32 bytes).
- **MAC input.** `mac` = HMAC-SHA-256(key[kid], `"cratis-dpop-nonce-v1"` ‖ `0x00` ‖ ver ‖ domain ‖ kid ‖ issued_at ‖ random ‖ u16be(len(origin)) ‖ origin ‖ jkt). `origin` is the configured public origin, ASCII, as serialized for `htu`; `jkt` is the 32-byte SHA-256 JWK thumbprint decoded from the proof key. Origin and `jkt` are authenticated but not carried. Every nonce is issued after the proof checks pass, so `jkt` is known at issue time.
- **Validation order.** Length and version; domain matches the validator's role; `kid` in its verify set; MAC in constant time; then `issued_at <= now + 30` and `now < issued_at + 330`. An unknown `kid` or any failure gets a `use_dpop_nonce` challenge with a fresh nonce; the request is never forwarded.
- **Keys.** Each nonce domain and environment has its own set of 256-bit random keys in the secret store, never shared with access-token or envelope keys; Identity and AuthProxy never share a key.
- **Distribution and rotation.** Validators read the key set (one active `kid` plus verify-only `kid`s) from the secret store and cache it for at most 60 seconds. Routine rotation is every 24 hours: a new key is published verify-only at least 120 seconds before it becomes active, and the previous key stays verifiable for at least 390 seconds after its last issuance (1 hour recommended).
- **Emergency retirement.** A compromised nonce key is retired with no overlap through an invalidation barrier: advance a key-set epoch in the secret store and push an invalidation to every validator; each replica drops its cached key set and stops issuing and accepting nonces until it has loaded the replacement; unreachable replicas are quarantined until they confirm. Clients recover with one extra `use_dpop_nonce` round trip. A replica that cannot load its key set fails closed.

For resource failures, AuthProxy MUST return HTTP 401 without a caller or downstream dispatch:

| Failure | Challenge |
|---|---|
| Bound token sent as Bearer, missing/invalid proof, mismatch, replay or cache unavailable | `WWW-Authenticate: DPoP error="invalid_dpop_proof", algs="ES256"` |
| Missing, stale or invalid required nonce after other proof checks pass | `WWW-Authenticate: DPoP error="use_dpop_nonce", algs="ES256"` plus fresh `DPoP-Nonce` |
| Invalid access token or unbound token under DPoP / required-binding policy | `WWW-Authenticate: DPoP error="invalid_token", algs="ES256"` |

DPoP-route challenges MUST also include the configured RFC 9728 `resource_metadata` parameter, as in [access-token v1 §7](identity-access-token-v1.md#7-http-errors-and-protected-resource-metadata). Invalid ordinary unbound Bearer requests retain that profile's Bearer errors; credential ambiguity retains the MCP profile's HTTP 400. Once a bound token is identified, challenges MUST NOT suggest Bearer downgrade. Error descriptions MUST NOT expose raw tokens, proofs, keys or internal infrastructure. Insufficient scope on a valid DPoP credential remains HTTP 403 with DPoP `error="insufficient_scope"`, required `scope`, `algs="ES256"` and `resource_metadata`.

## 6. Envelope v2 and Arc

AuthProxy MUST forward the result only after successful validation, covered by the signed v2 envelope along with the verified credential, method, path, audience, constraints and policy epoch. It MUST strip untrusted inbound identity/result headers. This profile defines logical semantics, not new envelope field spellings:

| Result | Covered evidence / Arc mapping |
|---|---|
| Bound | Verified `jkt` and accepted proof `jti`; maps to [`senderBinding`](arc-verified-caller-v1.md) containing those values. |
| Unbound | Explicit no-binding result; maps to no `senderBinding`. MUST NOT fabricate a key or claim proof validation. |

Arc MUST verify envelope v2, audience, freshness, transport and applicable envelope replay rules before trusting the result. Missing/unknown result semantics MUST fail, not be interpreted as successful DPoP. Arc MUST NOT re-validate the client proof on the verified-envelope path, and raw proofs MUST NOT be forwarded as evidence. The proof replay cache and envelope replay protection are separate requirements.

Arc MUST enforce current “bound required” policy using this trusted result, including policy-epoch re-evaluation; unbound callers MUST be refused when binding is required. It MUST retain tenant, environment, membership, grant, scope and fence checks. A failed edge proof MUST produce neither envelope nor caller. This aligns with [verified-caller v1](arc-verified-caller-v1.md) and [MCP v1's no-downgrade rule](arc-mcp-authorization-v1.md); optional client support never overrides an existing binding.

## 7. MCP compatibility and vectors

DPoP support in **Claude Code, claude.ai, Codex and ChatGPT is UNVERIFIED**, not established absence of support ([standards/client review, Identity #34](https://github.com/Cratis/Identity/issues/34)). MCP DPoP MUST remain opt-in when organisation policy permits unbound tokens. An organisation requiring DPoP cannot use a client lacking support: issuance MUST fail clearly as specified in §2, not issue Bearer tokens or silently disable policy. A client already holding a bound token MUST still prove possession, regardless of optional MCP support.

[JSON vectors](../../Source/Identity.Specs/Vectors/identity-dpop-v1/) are **data only**, not executable conformance or real credentials. Each has `name`, `description`, `given`, `when`, `then`. Unless overridden, the trusted fixture supplies: time `2000000000`, configured origin `https://direct.cratis.io`, external escaped path `/mcp`, method `POST`, internal host `http://authproxy.internal:8080`, replica `edge-a`, shared cache healthy/empty, optional binding policy, and an otherwise valid access token under access-token v1 with `cnf.jkt=xx0BcA-wMohw8atYDJOe6peGModklG2wRHBlXHMvl0M`.

The synthetic token bytes for `ath` calculations are the ASCII string `synthetic-access-token`; they stand in for the token's serialized bytes, not a parseable JWT. Their SHA-256 base64url hash is `F30iRba6HrejUQBwrzD-RojRUlUkFovd8PoQVXZ9_xY`. The default decoded proof is the `valid-bound-request` vector's `given.proof`. Other vectors' decoded `header`/`payload` objects override members of that fixture; `jwk` is replaced as a whole when supplied, and null removes a member. `signature` is always a simulated `valid`/`invalid` result. `nonce-current` denotes a valid HMAC nonce for this origin/key/domain, issued at `1999999990`; `nonce-stale` denotes one issued at `1999999600`. Labels are not real nonce encodings, except in `nonce-encoding.json`, which carries real §5.1 encodings under a documented test-only key. These defaults apply only to data vectors, not runtime validation.

A future executable suite MUST supplement these vectors with real signing, hashing, malformed parsing, time/TTL boundaries, AS refresh binding, atomic cross-replica races, cache loss/recovery, nonce domain separation and rotation, policy changes and signed-envelope tampering. JSON parsing alone proves syntax, not security conformance.

## 8. Decisions recorded at sign-off

- **Owners/sign-off:** signed off in Identity #14 (2026-11-05).
- **Operations:** decided in Identity #14: the 300-second proof/nonce windows, 30-second future allowance and per-validator clock offset bound, 60-second maximum inter-validator difference, 390-second replay TTL and cache-loss quarantine (`300 + 30 + 60`), shared-cache durability/capacity and fail-closed recovery procedures. W2 implementation proof is still required for replica races/restarts, clock-offset refusal and key overlap.
- **Nonce/envelope wire:** the HMAC nonce encoding and key distribution/rotation are decided in §5.1; the exact signed v2 bound/no-binding serialization is locked with the envelope contract ([arc-forwarded-identity-v2](arc-forwarded-identity-v2.md)); logical `senderBinding` already includes accepted proof `jti`. The `nonce-encoding.json` vector covers valid, wrong domain, wrong origin, wrong `jkt`, unknown `kid`, expired and future nonces, with both time boundaries.
- **Algorithm difference:** Design A/B mention ES256 and EdDSA; this narrower v1 intentionally permits ES256 only. The owners accepted this profile choice in Identity #14; EdDSA needs a reviewed profile change.
- **MCP capability:** verify named clients and registration/capability UX with #34; do not infer support or make DPoP universal meanwhile.
- **Inherited contracts:** registration and current-policy resolution/freshness, refresh-family durability and native-verifier rollout require implementation proof. `cnf` is already in contract v1; no allowlist change is needed. Section 2 explicitly requires DPoP-bound delegated-actor exchange and confidential organisation-automation issuance with `private_key_jwt`, matching [verified-caller v1's principal requirements](arc-verified-caller-v1.md#credential-matrix); optional MCP support does not relax those requirements.

## 9. References

- [RFC 9449 — DPoP, especially §§4–8](https://www.rfc-editor.org/rfc/rfc9449)
- [RFC 7638 — JSON Web Key thumbprint](https://www.rfc-editor.org/rfc/rfc7638)
- [RFC 9728 — Protected resource metadata](https://www.rfc-editor.org/rfc/rfc9728)
- [MCP authorization, 2026-07-28](https://modelcontextprotocol.io/specification/2026-07-28/basic/authorization)

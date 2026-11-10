# JWT access-token profile

```text
Spec id: identity-access-token
Version: 1
Status: Signed off
Signed off: 2026-11-05 by woksin, https://github.com/Cratis/Identity/issues/14; owner confirmation pending
Issue: https://github.com/Cratis/Identity/issues/28
Sign-off issue: https://github.com/Cratis/Identity/issues/14
Supersedes: None
```

## 1. Scope and authority

Keywords follow [the specification conventions](README.md). The [Terms of identity-contract-v1](identity-contract-v1.md#terms) apply **verbatim**; this document does not redefine them. Its person, resource, tenant and claim rules remain prerequisites. This specification defines one validation profile for AuthProxy, Arc and products that validate Identity person OAuth access tokens natively. It does not cover PATs, workload credentials, ID-token validation or signed proxy envelopes. Credential dispatch MUST select the verifier once, with no fallback after failure (Design A D3/D11).

This is a signed-off contract (Identity #14, 2026-11-05), not deployed behavior. The [event contract](identity-events-v1.md) governs lifecycle facts; tokens, proofs and individual refresh rotations MUST NOT become event payloads.

## 2. Token shape and claims

### 2.1 JOSE header

Tokens MUST be signed-only compact JWS JWTs (three parts), never encrypted JWE tokens (five parts), including nested signed-and-encrypted tokens.

| Header | Issuer requirement | Verifier requirement |
|---|---|---|
| `typ` | MUST emit `at+jwt`. | MUST require `at+jwt` or `application/at+jwt`; no absent type or generic `JWT`. |
| `alg` | MUST use `ES256` with a P-256 signing key. | Allow-list is exactly `ES256`; MUST reject every other algorithm, including `none`, all `HS*`, and algorithms advertised only by the token. |
| `kid` | MUST identify the realm's published signing key with a nonempty string. | MUST resolve exactly one compatible signing key in the trusted issuer's JWKS (§6). |

ES256 gives asymmetric verification without distributing a signing secret to resource servers and uses a widely supported curve. A single algorithm reduces algorithm-confusion and rollout ambiguity. This is a profile choice, not an RFC 9068 mandate; PS256 or another algorithm needs a reviewed profile change. Verifiers MUST NOT use token-supplied `jku`, `x5u` or embedded `jwk` as trust anchors, and MUST reject unsupported critical headers.

### 2.2 Claim contract

The [contract v1 §5 claim table](identity-contract-v1.md#5-token-claims) is reused verbatim below, including its requiredness and review status. `R` means required; `C` means required under the stated condition; `O` means optional. NumericDate values are integer UTC seconds since the Unix epoch. **Review** marks a mapping that Direct/Studio owners reviewed and accepted in the Identity #14 sign-off.

| Claim | JSON type | Presence | Source and meaning | Review |
|---|---|---|---|---|
| `iss` | string | R | Configured realm issuer (§2.2); part of the person key. | Review |
| `sub` | string | R | Account UUID (§2.1), always the person, never an agent or upstream login. | Review |
| `aud` | string | R | Resource catalog audience; exactly one resource (§4). | Review |
| `exp` | integer | R | Issuer's short-lived expiry, constrained by grant/root expiry. Default 300 seconds, at most 600 seconds (§4). | Review |
| `iat` | integer | R | Issuer's token issuance time; MUST precede `exp`. | Review |
| `jti` | string | R | Issuer-generated opaque, unique token identifier, not a secret. | Review |
| `client_id` | string | R | Registered OAuth client identifier; no assertion that a public client is authenticated. | Review |
| `scope` | string | R | Space-delimited, catalog-valid scope ceiling from consent/grant and current policy. | Review |
| `tid` | string | C: tenant-bound resource | Selected product tenant id (§4). This is the **tenant** claim; `tenant_id` is not an additional alias. Person tokens authorizing workspace operations MUST carry it. | Review: name and legacy mapping |
| `org` | string | O: organisation relevant | Access's opaque organisation id from a confirmed binding, never inferred from `tid`; not membership. Identifier format remains open; MUST NOT be used for authorization until agreed. | Review |
| `idp` | string | R for upstream-authenticated person | Trusted connection's exact upstream issuer/authority used for this authentication, not every linked provider. | Review |
| `idp_sub` | string | C: upstream authentication | Immutable upstream subject verified by that connection. Listed privacy exception for migration/assurance; not the realm `sub`. | Review: exposure/minimization |
| `auth_time` | integer | R for person authentication | Actual verified person-authentication time; refresh/exchange MUST preserve it. | Review |
| `acr` | string | R for person authentication | Connection-mapped assurance level; unknown evidence is `basic`. Level vocabulary beyond `basic` remains open. | Review |
| `amr` | array of strings | O | Verified, connection-mapped methods; omit if unknown, never copy untrusted methods as elevated evidence. | Review |
| `act` | object | C: delegated/on-behalf | Current actor's registered `iss` and `sub` strings; optional nested `act` records earlier actors. Top-level `sub` remains the person. Only current actor and top-level claims authorize; prior actors are informational (RFC 8693). Actor namespaces/registration need review. | Review |
| `cnf` | object with `jkt` string | C: DPoP-bound | Issuer-verified DPoP public-key thumbprint (RFC 9449); a bound token MUST NOT be accepted without a valid matching proof or as plain bearer. | Review |
| `github_id` | string | C: GitHub migration consumer | Positive decimal immutable GitHub id, equal to `idp_sub` when this authentication is GitHub; emitted only to registered migration consumers. Removal requires coordinated migration. | Review: retirement and audience |

Section references **inside this copied table refer to contract v1**, not this document. This profile does not downgrade any required or conditional claim. No roles, membership, email, display name, login or arbitrary upstream claims are permitted. The only exception is the display-only `preferred_username` of [contract v1 §5.1](identity-contract-v1.md#51-display-claim-preferred_username), and only for a resource whose catalog entry opts in. Verifiers MUST NOT use it for authorization, matching or account resolution. Additional claims require contract review/versioning. `nbf` is not currently in that allowlist; see §9. DPoP binding and proof validation are detailed in **identity-dpop-v1**; delegation semantics remain those of contract v1 and RFC 8693.

## 3. Resource selection and audience

A client MUST send the registered resource indicator as `resource` in authorization and token requests. RFC 8707 requires an absolute URI without a fragment; contract v1 §4 further restricts it to a registered HTTPS URL. The authorization server MUST resolve it through the trusted resource catalog, with a one-to-one mapping between resource indicators and audience slugs, plus the corresponding scope prefix. An unknown or unapproved resource MUST be refused with OAuth `invalid_target`, not echoed into a token.

For example, `resource=https://direct.cratis.io` (a REST API resource) maps to `aud=direct-api`, and the Direct MCP endpoint's resource maps to `aud=direct-mcp`; neither maps to an audience containing the URL. This follows Design A §4.11, Scope catalog (N1), and §4.4. The token MUST have exactly one `aud` string, matching contract v1 §4. Arrays, including singleton arrays, and multi-audience tokens MUST be rejected. A verifier MUST compare that string exactly against its own trusted catalog entry; it MUST NOT derive its expected audience from the request's Host header, token or caller-supplied resource.

A client needing several resources MUST obtain and store a separate token per resource, using a separate resource-specific request (or authorized exchange under its separate contract). This profile does not issue a combined multi-resource token. Scope ceilings, tenant constraints and live product membership checks still apply; an audience match alone grants nothing.

## 4. Lifetimes and refresh

| Setting | v1 rule |
|---|---|
| Access-token lifetime | Default **300 seconds**; issuer MUST enforce `0 < exp - iat <= 600` seconds, shortened by grant/root expiry and policy. |
| Clock skew | Verifiers MUST use **30 seconds** uniformly for time comparisons (§5), not extend the minted lifetime. No additional library-default tolerance may be stacked on it. |
| Refresh rotation | Every successful refresh MUST atomically consume the old refresh token and issue its replacement. Reuse MUST revoke the whole refresh family; there is no reuse grace window in v1. |
| Refresh absolute lifetime | **30 days** (decided at sign-off) from family creation; rotation MUST NOT reset it. |
| Refresh idle lifetime | **7 days** (decided at sign-off) since last successful redemption, capped by the absolute deadline. |

Refresh MUST preserve actual `auth_time` and MUST NOT widen resource, tenant, scope, assurance or grant authority. Policy MAY shorten lifetimes, never extend these ceilings. Family revocation, concurrent redemption, durable fences and recovery are **detailed in identity-revocation-v1**. Refresh-family revocation alone does not instantly invalidate offline-validated JWTs: they remain bounded by expiry plus skew unless a separate revocation check refuses them. Sensitive actions need the separately specified live grant/policy checks.

## 5. Ordered resource-server validation

Before validation, the server MUST select the trusted realm, resource catalog entry, issuer metadata/JWKS configuration and current UTC time from deployment configuration, never from unverified token claims. Multiple permitted realms require an explicit issuer allow-list; an unverified `iss` may only select an already trusted entry. Verifiers MUST run the following checks in order and stop at the first failure. No principal or authorization decision may use unverified claims.

All failures in this table produce **401 `invalid_token`**, with the challenge in §7. Reason ids are diagnostic/vector vocabulary, not additional public OAuth errors. If a key fetch cannot complete, validation fails closed; cached, unexpired trusted keys may still be used under §6.

| Order | Required check | Failure reason |
|---|---|---|
| 1 | Require a three-part compact JWS, valid header/payload JSON objects, no duplicate member names, and supported critical headers. MUST NOT decrypt or accept JWE, even with a valid inner JWT. | `encrypted_token` for five-part JWE; otherwise `malformed_token` |
| 2 | Require `typ` as allowed by §2.1. | `invalid_typ` |
| 3 | Require `alg=ES256`, independently of what the key/token advertises. | `invalid_alg` |
| 4 | Require a nonempty string `kid`; select one P-256 public signing key from the trusted issuer's JWKS, compatible with ES256 and signing use (`use`/`key_ops` if present). Unknown `kid` triggers at most one bounded refresh (§6), then rejection if still unknown. Ambiguous or incompatible keys fail. | `missing_kid`, `unknown_kid` or `invalid_key` |
| 5 | Verify the JWS signature with that key and algorithm, not a token-supplied key or symmetric fallback. | `invalid_signature` |
| 6 | Require string `iss` exactly equal to the configured issuer (including path/trailing slash). No normalization or alternate issuer accepted. | `invalid_issuer` |
| 7 | Require `aud` to be one string exactly equal to this resource's catalog audience. An ID-token client audience or resource-indicator URL does not match. | `invalid_audience` |
| 8 | Require integer `exp` and `iat`, `0 < exp - iat <= 600`, `now < exp + 30` and `iat <= now + 30`. If `nbf` is present, require an integer, `nbf < exp` and `nbf <= now + 30`; this does not authorize emitting that claim (step 12). | `invalid_time` |
| 9 | Require `sub` in the exact lowercase UUID-v4 format of contract v1 §2.1. Never trim, lowercase or repair it. | `invalid_subject` |
| 10 | Require a nonempty string `client_id`. It identifies the registered OAuth client, not proof that a public client authenticated. | `missing_client_id` or `invalid_client_id` |
| 11 | Require a nonempty string `jti`. Issuer MUST generate unique opaque identifiers; Guid identifiers are acceptable. | `missing_jti` or `invalid_jti` |
| 12 | Enforce every remaining type, required/conditional presence, format and allowlist rule in contract v1 §5 for the trusted resource/grant context, including `scope`, `tid`, upstream evidence and person assurance. Claims outside that allowlist, including `nbf` (not in the contract v1 §5 allowlist), fail. | `invalid_claims` |
| 13 | If `cnf` is present, require matching sender binding and the proof-validation/replay rules of identity-dpop-v1. A DPoP-bound token MUST NOT be accepted as plain bearer, or when proof support is unavailable. | Binding/proof errors and DPoP challenges are detailed in identity-dpop-v1, not the bearer challenge below. |

Only after these checks may the server construct a verified caller and apply request target/tenant, live membership, grant, assurance and scope authorization. A missing scope needed for an operation on an otherwise valid credential is `insufficient_scope` (§7), not a token-format error.

An **ID token is never an access token**. Both the type check and audience check are mandatory independent defenses: ID tokens use their OIDC type (commonly `JWT`) and their audience is the OAuth `client_id`, not the resource's catalog audience. Verifiers MUST NOT disable either check even when the other already prevents an example attack.

Bearer access tokens are intentionally usable on multiple requests until expiry. A single-use replay cache for the access-token `jti` is therefore **not required** and would reject legitimate reuse; its presence is not replay prevention. DPoP **proof** `jti` values instead require replay detection shared across replicas, detailed in identity-dpop-v1. Token ids and proof ids MUST NOT be confused.

## 6. Metadata, JWKS and rotation

A resource server MUST obtain `jwks_uri` from its trusted issuer's OIDC/OAuth metadata over authenticated HTTPS and verify the metadata's issuer exactly. It MUST NOT discover an arbitrary issuer or key URL from a token. Metadata and key fetches MUST have bounded timeouts and sizes; unavailable trusted configuration MUST fail closed, without token-kind or algorithm fallback.

v1 cache/rotation policy:

- Cache successful metadata/JWKS responses for at most **300 seconds**, honoring shorter HTTP freshness or `no-cache`/`no-store`. Revalidate proactively at expiry; do not use an expired cache if revalidation fails.
- An unknown `kid` permits **one** refresh attempt for that validation, then one lookup and rejection if unresolved. Coalesce concurrent fetches and rate-limit unknown-key-triggered refreshes to **one per issuer per 30 seconds**, including negative results. A request arriving during the limit uses the fresh cache or fails; it does not queue unbounded fetches. Never retry a bad signature with other keys.
- Publish a new public key at least **600 seconds before first signing use**, allowing the 300-second metadata and 300-second JWKS cache bounds to elapse. Maintain both old and new keys during rollover; never reuse a `kid` for different key material.
- After the last token signed by the old key, retain its public key for at least the maximum access-token lifetime plus skew (**630 seconds**). If that key also signs other artifact types, their longer validation windows govern retention. Retire signing use before removing the public key; do not delete keys needed by still-valid artifacts.
- Emergency compromise rotation is not routine rollover. It requires explicit invalidation/revocation policy and fail-closed recovery; see [identity-revocation-v1 §6.1](identity-revocation-v1.md#61-independent-key-and-client-secret-retirement-tombstones) for restore-safe tombstones and the [#18 security runbooks](../security/runbooks/) for the procedure. Ordinary cache freshness does not promise immediate revocation.

Private signing keys MUST stay at the issuer/secret store, never in resource-server configuration, vectors or events. Identity MUST test publication, overlap, retirement and unknown-key failure across replicas.

## 7. HTTP errors and protected-resource metadata

OAuth-capable machine routes MUST NOT redirect to an HTML login page. For a missing credential, return 401 and a Bearer challenge with `resource_metadata`, without an OAuth error value. For an invalid bearer token, return:

```http
HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer error="invalid_token", resource_metadata="https://direct.cratis.io/.well-known/oauth-protected-resource"
```

For a valid token lacking the operation's required scope, return:

```http
HTTP/1.1 403 Forbidden
WWW-Authenticate: Bearer error="insufficient_scope", scope="direct:write", resource_metadata="https://direct.cratis.io/.well-known/oauth-protected-resource"
```

`resource_metadata` MUST identify the configured protected resource's RFC 9728 metadata, not an issuer metadata URL or a URL from the request. For path-based resource indicators, use RFC 9728's well-known path insertion rules. The `scope` parameter on insufficient-scope responses MUST identify the required catalog scopes. Do not disclose raw tokens, personal claims or key-fetch diagnostics in challenges. This follows RFC 6750 and **MCP 2026-07-28** authorization behavior, with RFC 9728 resource metadata on challenges. DPoP-bound credential challenges are detailed in identity-dpop-v1 and MUST NOT silently downgrade to Bearer.

## 8. OpenIddict 7.8.0 implementation notes (non-normative)

[OpenIddict token formats](https://documentation.openiddict.com/configuration/token-formats) and the verified 7.8.0 server-handler behavior establish feasibility, not conformance of an unconfigured server:

| Behavior | Configuration/issuance action needed |
|---|---|
| Access tokens are encrypted by default. | Call `DisableAccessTokenEncryption()` on server options so access tokens are signed-only JWSs for external resource servers. This does not disable signing. |
| Access tokens always have `typ=at+jwt`. | Keep the access-token pipeline; do not reuse ID-token or generic JWT validation. Test the emitted header. |
| Signing algorithm/key selection depends on credentials. | Register the published ES256/P-256 signing credential and stable `kid`; ensure no other access-token signing credential can select an unprofiled algorithm. |
| `client_id` is emitted only "if available". | Require a resolved registered client for every issuance path, including refresh/exchange; propagate its protocol client identifier into the issuance context/principal consumed by the access-token handler. Refuse issuance if unavailable, and assert the final token has the exact `client_id`. Do not rely on the conditional default or accept a claim supplied by the caller. |
| `jti` is a Guid. | Preserve unique server-generated ids; do not substitute grant/family ids. |
| Default `aud` derives from the principal's resources. | Resolve the request's resource URI through the catalog, then set the token audiences from that catalog (e.g. `principal.SetAudiences(catalogAudience)`), not by echoing the resource URI. Ensure the final issuance handler does not overwrite that mapping from resources. Conformance MUST inspect the serialized token: `resource=https://direct.cratis.io` yields string `aud=direct-api`, with no second audience or array. |
| OpenIddict adds private `oi_`-prefixed claims to principals it serializes. | Strip every `oi_`-prefixed claim from the access-token principal before serialization (claim destinations or a final issuance handler), so the token satisfies the §5 step 12 allowlist. Conformance MUST assert the serialized access token contains no `oi_` claim; verifiers stay strict and reject such tokens. |
| Claim destinations and lifetimes are configurable. | Explicitly select only contract v1 access-token claims and their conditions, preserve person `auth_time`, set the five-minute default/ten-minute ceiling and refresh policies, and disable default verifier skew in favor of this profile's 30 seconds. |

Executable conformance coverage MUST verify the actual OpenIddict output, including catalog mapping, unknown-resource `invalid_target`, required client id on every issuance path, single-string audience serialization, no JWE, and rotation/reuse behavior. A setter call alone is not evidence that the final handler emits this profile.

### Token-exchange issuance profile

An access token issued by RFC 8693 token exchange (see [Token exchange](../token-exchange.md)) follows this profile with these rules:

- `aud` is the single catalog audience of the requested resource, never the subject token's audience.
- `tid` is the tenant the client requested, and only when the product lists it for the person on that resource at issue time.
- `sub` is the Identity account subject; `azp` and `client_id` are the exchange client.
- `idp` is the configured external issuer that signed the subject token.
- `auth_time` is copied only when the subject token carried one; it is never invented.
- There is no `act` or `cnf` claim and no refresh token.

## 9. Decisions recorded at sign-off

- **Audience representation:** aud as catalog slug vs resource-indicator URI (MCP 2026-07-28 requires only that the server validates tokens were issued for it; slug chosen by design N1). Decided in Identity #14: one exact catalog slug, preserving contract v1, with the mapping and OpenIddict handler ordering as specified here.
- **Algorithm/operations:** decided in Identity #14: ES256-only, the 300-second cache bounds, 30-second refetch rate limit, 600-second prepublication and 630-second minimum old-key retention; KMS/library support across consumers and emergency invalidation policy are implementation evidence for W2.
- **Lifetimes:** decided in Identity #14: 300-second access-token default, 600-second ceiling and 30-second skew; refresh absolute **30 days** and idle **7 days**.
- **Additional claim:** `nbf` is not in contract v1 §5. This profile defines defensive time handling but does not authorize issuance or acceptance under the current allowlist. Adding it needs a separately reviewed contract change; otherwise omit it. This was settled at the Identity #14 sign-off.
- **Inherited decisions:** contract v1's consumer review, actor namespaces, assurance vocabulary, tenant/`tid` and upstream/migration privacy decisions were signed off in Identity #14, with exact issuers per D1 (option B, Identity #21) and organisation per D5 (option A, Identity #23); this profile does not itself restate them.
- **Dependent contracts/sign-off:** identity-dpop-v1 and identity-revocation-v1 (including family reuse/concurrency and durable fences) are aligned; signed off in Identity #14 (2026-11-05). No instant offline-JWT revocation or runtime conformance is claimed here.

## 10. Data vectors and references

[JSON vectors](../../Source/Identity.Specs/Vectors/identity-access-token-v1/) are **data only**, not executable specs or real credentials. Each file has `name`, `description`, `given`, `when` and `then`. Validation vectors describe decoded `header`/`payload`, `serialization`, `parts` and a `signature` result (`valid`, `invalid` or `wrong_key`); these stand in for cryptographic fixtures, not instructions to bypass signature checks. The encrypted case has no accessible payload; its `signature` result describes a hypothetical valid inner JWS, which MUST NOT trigger decryption or acceptance. Issuance vectors describe a resource request and expected decoded output/error instead.

Unless a case overrides them, a future harness MUST supply this trusted fixture: issuer `https://identity.example.test`, resource indicator `https://direct.cratis.io`, audience `direct-api`, scope `direct:read`, tenant-bound upstream-authenticated person context, metadata URL `https://identity.example.test/.well-known/openid-configuration`, JWKS URI `https://identity.example.test/jwks`, fresh metadata/keys and the single compatible P-256 signing key `key-1`. Current time is `2000000000` UTC seconds and skew is 30 seconds. For `unknown_kid`, the one allowed JWKS refresh succeeds but still contains only `key-1`; the refresh budget is available. Tokens and subjects are synthetic. Acceptance means token validation only, not live membership or operation authorization. `then.reason` names the first failing step; public errors remain those of §7.

Implementation MUST consume these data in executable specs and supplement them with real signing/verification, malformed parsing, key compatibility, all claim conditions, exact time boundaries, metadata outages/cache rotation and DPoP coverage. In particular, independently exercise the audience defense using `typ=at+jwt` with a client-id audience, not only an ID token rejected earlier by type. JSON parsing proves syntax only.

- [RFC 9068 — JWT profile for OAuth 2.0 access tokens](https://www.rfc-editor.org/rfc/rfc9068)
- [RFC 8707 — Resource indicators](https://www.rfc-editor.org/rfc/rfc8707)
- [RFC 6750 — Bearer token usage and errors](https://www.rfc-editor.org/rfc/rfc6750)
- [RFC 9728 — Protected resource metadata](https://www.rfc-editor.org/rfc/rfc9728)
- [RFC 8693 — Token exchange and actor claims](https://www.rfc-editor.org/rfc/rfc8693)
- [RFC 9449 — DPoP](https://www.rfc-editor.org/rfc/rfc9449)
- [MCP authorization, 2026-07-28](https://modelcontextprotocol.io/specification/2026-07-28/basic/authorization)

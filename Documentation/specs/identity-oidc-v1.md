# OpenID Connect ID-token profile

```text
Spec id: identity-oidc
Version: 1
Status: Draft
Issue: https://github.com/Cratis/Identity/issues/58
Sign-off issue: https://github.com/Cratis/Identity/issues/14
Supersedes: None
```

## 1. Scope and authority

This profile defines ID tokens and UserInfo for confidential relying parties signing a person into an application. Keywords follow [the specification conventions](README.md). The [person and account contract](identity-contract-v1.md) supplies the canonical subject; this is a separate profile from [resource access tokens](identity-access-token-v1.md). An ID token MUST NOT authorize a product API, select a tenant, or act as a resource credential.

OIDC issuance is disabled by default. An operator MUST configure the public issuer, register confidential clients, record `Oidc:ConsentDecision` as `ImplicitFencedPath` or `ActivationBarrier`, and explicitly enable `Oidc:IssuanceEnabled`. The recorded decision does not itself wire the dormant consent-activation state machine. The protocol core has no runtime dependency on the session/logout lane. Deployment owners MUST complete session/logout composition before enabling a relying party that needs it.

This Draft requires maintainer and relying-party review before sign-off. Session/logout behavior is a separate composition boundary; its implementation MUST NOT be inferred merely from the discovery field naming its endpoint.

## 2. Authorization and grant boundaries

Relying parties MUST use authorization code flow with S256 PKCE, an exact registered callback, state and a fresh nonempty nonce. Client authentication is REQUIRED at the token endpoint. Native clients and resource-server introspection clients do not gain OIDC permissions merely because the server enables the profile.

Identity-only requests contain `openid`, optionally `profile` and `offline_access`, without a resource indicator or resource-owned scope. Granted scopes MUST remain within both the request and the registered client ceiling. Requests exceeding that ceiling are refused with `invalid_scope`; they MUST NOT silently acquire profile data. Unknown scopes and identity scopes for non-OIDC clients are refused. An identity-only request with a resource indicator is refused with `invalid_target`. Requests mixing `openid` with resource-owned scopes are refused with `invalid_scope` before login or consent. Resource-only requests retain the existing resource-grant policy.

Identity-only consent has no resource or tenant selector and is protected by the existing antiforgery mechanism. There is no remembered consent: `prompt=none` yields `login_required` without an acceptable session and `consent_required` when interactive approval remains necessary. `prompt=login` requests upstream authentication; after login, only the satisfied `login` prompt is removed from the continuation.

`max_age` is supported. A malformed value (not a non-negative integer) is refused before upstream login with `invalid_request` and description `max_age is invalid`. A session whose `auth_time` is older than `max_age` seconds is not reused: with `prompt=none` the answer is `login_required`, otherwise Identity re-authenticates upstream. `max_age=0` is equivalent to `prompt=login`. After the upstream callback the continuation request has `max_age` stripped entirely, because login just happened. `auth_time` is when Identity verified the upstream callback; an upstream such as GitHub cannot be forced to re-prompt. ID tokens carry the verified `auth_time`, preserved on refresh.

## 3. JOSE and key selection

ID tokens MUST be signed-only compact JWS tokens with `typ=JWT`, `alg=RS256`, and a nonempty `kid` naming the compatible RSA key in the issuer's JWKS. Encryption, `none`, symmetric algorithms, ES256 ID-token fallback, and token-provided key trust anchors are not part of this profile.

An RSA signer MUST have at least 2048 bits and satisfy `NotBefore + 600 seconds <= now < NotAfter`. Selection uses the newest eligible activation, breaking ties by ordinal `kid`, and MUST NOT fall back to an expired or prepublication key. When issuance is enabled, configured OIDC clients without an eligible RSA signer fail startup. Development ephemeral RSA keys satisfy this requirement without a deployed ring.

Every issuance resolves readiness and a credential atomically from one current published snapshot under the ring lock. A failed key reload, expired signer, or EC-only replacement MUST refuse ID-token issuance without degrading existing OAuth signing behavior. Restoring eligible RSA material permits subsequent issuance without restart. A mixed RSA/EC ring advertises only RS256 for ID tokens; it does not change the access-token algorithm selection.

## 4. Claim contract

NumericDate values are integer UTC seconds. The default lifetime is 300 seconds; `Tokens:IdentityTokenLifetime` MUST be positive and no greater than one hour.

| Claim | JSON type | Presence and meaning |
| --- | --- | --- |
| `iss` | string | Required; configured public issuer, exactly. |
| `sub` | string | Required; active canonical account identifier. |
| `aud` | array of strings | Required; exactly `[client_id]`, including for a single audience. |
| `azp` | string | Required; the authenticated relying-party client identifier. |
| `iat`, `exp` | integer | Required; current issuance and expiry, with `iat < exp`. |
| `jti` | string | Required; fresh opaque token identifier, independent of credential aliases. |
| `auth_time` | integer | Required; original verified upstream authentication time, preserved on refresh. |
| `nonce` | string | Required on initial sign-in; exact authorization-request nonce. Omitted on refresh. |
| `sid` | string | Included only when supplied by the accepted site authentication; never fabricated by the protocol core. |
| `at_hash` | string | Required when an access token is returned; base64url left half of SHA-256 of that same response's access-token ASCII bytes. |
| `name`, `preferred_username` | string | Included only when `profile` was granted; current canonical profile values. |

ID tokens MUST NOT carry email, raw GitHub id/login fields, `idp_sub`, tenant identifiers, `cid`, `credential_id`, roles, or scope. `acr` and `amr` are omitted until a separate assurance contract defines them. A relying party MUST verify signature, issuer, audience, authorized party, lifetime and initial nonce, and MUST NOT dispatch an ID token to an access-token verifier after failure.

## 5. UserInfo

`GET` and `POST {RoutePrefix}/connect/userinfo` accept only a validated Bearer access token with the fixed logical audience `cratis-identity-userinfo` and granted `openid`. Native token validation and existing grant fences run before the endpoint reads current active-account data. ID tokens, refresh tokens, malformed tokens, resource-audience access tokens and inactive accounts MUST NOT produce a positive response. Unavailable admission state MUST fail closed.

The JSON response contains exactly `sub`, plus `name` and `preferred_username` when the access token's grant includes `profile`. Its `sub` MUST equal the ID token subject. The response MUST NOT reflect caller-supplied scope parameters or return upstream/tenant/credential metadata. Responses prevent storage. Invalid bearer credentials are refused with HTTP 401 and a Bearer challenge; the native protocol pipeline owns pre-endpoint rejection details.

## 6. Refresh

Refresh uses the existing rotation, atomic admission and revocation machinery. Identity-only refresh MUST NOT enter resource lookup or tenant selection. The client binding, UserInfo audience and original scope ceiling remain fixed; a request may narrow its scopes while retaining `openid`, but MUST NOT add scopes or a resource.

A successful refresh returns a newly signed ID token when `openid` remains granted. It preserves `iss`, `sub`, `aud`, `azp`, `auth_time` and any original `sid`; `iat`, `exp`, `jti`, and `at_hash` describe the new response. Profile values are reread from the current active account. The refreshed ID token omits nonce, including when a caller supplies a token-endpoint nonce parameter. Reuse fences the family, and no inactive or erased account may renew.

## 7. Discovery and configuration

`/.well-known/openid-configuration` is built from the configured issuer and endpoint prefix, not request or forwarded headers. Disabled issuance publishes only `issuer` and `jwks_uri`; the OAuth scope catalogue excludes `openid` and `profile`, and UserInfo is not mapped. Enabled metadata advertises code flow, public subjects, S256, confidential client authentication, the bounded scopes/claims above, and exactly `[RS256]` for ID-token signing. It advertises `max_age_supported: true`.

Authorization, token, UserInfo and composed end-session URLs use the configured route prefix. Discovery and JWKS stay at their unprefixed well-known paths. The provider omits `end_session_endpoint` by default. Session/logout composition must register its endpoint before explicitly opting discovery into advertising it; that implementation owns its behavior. See the [configuration reference](../../README.md#openid-connect-sign-in) for enabling a relying party and externally supplying its client secret.

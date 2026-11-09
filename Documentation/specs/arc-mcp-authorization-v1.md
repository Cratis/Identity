# MCP authorization

```text
Spec id: arc-mcp-authorization
Version: 1
MCP authorization revision: 2026-07-28
Status: Signed off
Signed off: 2026-11-05 by woksin, https://github.com/Cratis/Identity/issues/14; owner confirmation pending
Issue: https://github.com/Cratis/Arc/issues/3703
Sign-off issue: https://github.com/Cratis/Identity/issues/14
Supersedes: None
```

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Purpose

An MCP client needs a machine-readable authentication challenge, not a browser sign-in page. This specification describes PAT-only, OAuth-only, and combined hosting profiles. It does not claim tested compatibility with any external connector.

The MCP authorization revision is pinned to **2026-07-28** (confirmed in Cratis/Identity#34). The reference is the [MCP authorization specification](https://modelcontextprotocol.io/specification/2026-07-28/basic/authorization), based on OAuth 2.1 [draft-ietf-oauth-v2-1-13](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-v2-1-13). OAuth resource discovery follows [RFC 9728](https://www.rfc-editor.org/rfc/rfc9728); resource binding follows [RFC 8707](https://www.rfc-editor.org/rfc/rfc8707).

## Terms

Person, Account, Realm, Issuer, Subject, Linked key, Resource, Resource indicator, Audience, Tenant, Organisation, Environment, Actor, Credential kind, Assurance, Policy epoch, Access token, ID token, Refresh token, and PAT have exactly the meanings in the Terms table of [identity-contract-v1.md](identity-contract-v1.md), Cratis/Identity#24. Shared meanings are not redefined here.

A **PAT-only profile** enables only application PAT verification. An **OAuth-only profile** enables only OAuth resource access. A **combined profile** enables both on the same MCP path. **Protected-resource metadata** is the RFC 9728 document identifying this resource and its OAuth authorization servers. A **machine route** is an MCP API, challenge, discovery, or error route, not the authorization server's interactive sign-in page.

## Normative contract

MUST, MUST NOT, and SHOULD use [RFC 2119](https://www.rfc-editor.org/rfc/rfc2119) meanings. Requirements apply after sign-off.

### Profiles and challenges

| Profile | Missing or invalid credential | Discovery | Verification |
| --- | --- | --- | --- |
| PAT-only | MUST return 401 and a Bearer challenge without OAuth metadata | MUST NOT advertise an issuer or OAuth protected-resource metadata; an OAuth metadata request MUST return a machine-readable not-found response | Application PAT verifier only |
| OAuth-only | MUST return 401 and `WWW-Authenticate` pointing to RFC 9728 metadata | MUST publish resource metadata and configured authorization-server discovery | OAuth verifier only; PATs MUST be refused |
| Combined | MUST return 401 and the OAuth-capable metadata challenge | MUST publish OAuth metadata; PAT support MUST NOT be misrepresented as an OAuth issuer | Deterministic PAT dispatch plus OAuth verification on the same path |

Machine routes MUST NOT redirect to interactive login or return HTML, even when a browser identity provider is configured. A 401 MUST NOT be converted to a 302. Authentication failures MUST NOT disclose whether a specific PAT record exists. A valid authenticated caller lacking required authority MUST receive a machine-readable 403. In OAuth-capable profiles, an OAuth request lacking required scopes MUST receive `WWW-Authenticate: Bearer error="insufficient_scope", scope="...", resource_metadata="..."` on that 403. Clients SHOULD step up by requesting the union of previously granted and newly required scopes, with a finite retry limit. They MUST NOT retry indefinitely. Product-role denial MUST NOT be represented as resolvable solely by gaining scopes.

A request with more than one credential, or a credential in the URI query, MUST be refused with a machine-readable 400 before dispatch or caller construction (RFC 6750 §2 and OAuth 2.1). The server MUST NOT select a credential silently from multiple values. A DPoP proof accompanying its sender-bound token is not an additional access credential.

PAT-only is a static-header/minimal profile, not an assertion of OAuth interoperability with remote MCP connectors. It MUST NOT invent OAuth metadata to satisfy an OAuth-only client.

### OAuth discovery and resource binding

OAuth-capable MCP servers MUST implement RFC 9728 protected-resource metadata, and OAuth MCP clients MUST use it for discovery. The 401 challenge MUST include `resource_metadata` with the protected-resource metadata URL. That document MUST use the registered resource indicator in `resource`, the configured issuer in `authorization_servers`, and the [catalog](arc-scope-catalog-v1.md) scope names in `scopes_supported`. Resource metadata MUST NOT be confused with authorization-server metadata. Clients MUST verify that the protected-resource metadata `resource` equals the requested resource URL and MUST refuse a mismatch (RFC 9728 §3.3).

The client MUST send RFC 8707 `resource` with the intended resource indicator on both authorization and token requests. The authorization server MUST refuse unregistered resource indicators and MUST use the scope catalog's one-to-one mapping from the RFC 8707 resource indicator URL to its audience slug when binding the resulting access token. For example, `resource=https://direct.example.test/mcp` maps to `aud=direct-api`. Each audience slug MUST map back to exactly one resource indicator URL; `aud` MUST NOT be a URL. The resource verifier MUST validate issuer, the single catalog audience string (for example `direct-api`, not the resource URL), signature, expiry, `typ=at+jwt`, required access-token claims, applicable `tid` tenant binding, and sender binding where required, as defined in identity-contract-v1. It MUST NOT accept an ID token as an access token or forward an access token to a different resource.

The authorization server MUST require PKCE S256 and MUST reject `plain` and missing PKCE. Clients MUST use S256 and MUST refuse authorization when PKCE metadata is missing. Redirect URIs MUST match exactly. The only native loopback exception follows RFC 8252 for IP literals and variable ports, not `localhost` or wildcard hosts. Resource servers MUST NOT compensate for invalid tokens by falling back to forwarded browser identity. Successful credential verification MUST produce the same [caller contract](arc-verified-caller-v1.md) before product authorization.

Pre-registered first-party clients remain supported. Client ID Metadata Documents SHOULD be supported, pinned to [draft-ietf-oauth-client-id-metadata-document-03](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-client-id-metadata-document-03). Dynamic Client Registration is optional (MAY), for backwards compatibility only, subject to the registration security contract and tested connector needs. Connector compatibility MUST remain unverified until tested against supported versions.

DPoP is optional for MCP clients using unbound tokens; this contract MUST NOT impose it on every client. Client support is unverified. A sender-bound token carrying `cnf.jkt` MUST NOT be accepted as a plain bearer. Its verifier MUST validate the proof per RFC 9449 §4.3, including `typ=dpop+jwt`, signature, `htm` and `htu` matching the request, `ath` equal to the presented token's hash, and `iat` within a configured window. A missing, non-positive or unbounded `iat` window MUST cause refusal of sender-bound tokens; the window MUST be finite (recommended at most 5 minutes). The `jti` replay cache MUST retain entries at least for that window. The verifier MUST require a valid matching DPoP header and MUST refuse a missing header, a proof with the wrong key thumbprint, a replayed proof `jti`, or any invalid proof with 401. Optional client support MUST NOT disable an existing token binding.

### Combined dispatch

The combined profile MUST follow [PAT dispatch](arc-pat-format-v1.md). PAT namespace recognition MUST apply to the credential value under any Authorization scheme, including DPoP, and MUST match the whole `<key>_pat_` prefix ASCII case-insensitively, marker included. A PAT-namespace value under a non-Bearer scheme MUST be refused in the PAT branch, never passed to OAuth verification or introspection. A recognized PAT prefix under Bearer MUST select application verification; an invalid checksum, resource mismatch, or revoked PAT MUST NOT trigger OAuth fallback. Other bearer values MUST receive full OAuth verification rather than acceptance based on JWT shape. Both paths MUST preserve resource-local authority and apply current product policy.

Credentials are the Authorization header value, a session cookie, an AuthProxy identity envelope, and any credential in the URI query. A session cookie plus an Authorization bearer counts as two credentials and MUST be refused. An AuthProxy identity envelope plus Authorization MUST count as one credential only when the verified envelope cryptographically covers that exact Authorization value (forwarded identity v2 signs its SHA-256 hash, Cratis/Arc#3698); otherwise they MUST count as two and be refused. These rules apply to all profiles. AuthProxy MUST NOT attach a person envelope (an envelope whose credential kind is not `pat`) to a request it forwards with a PAT. Caller construction when an envelope accompanies a bearer MUST follow [Envelope and bearer together](arc-verified-caller-v1.md#envelope-and-bearer-together).

## Test vectors

All hosts and token markers are synthetic. `PAT_FIXTURE` and `OAUTH_FIXTURE` below mean test fixtures verified under the named path; they are not literal credentials.

### PAT-only

```http
GET /mcp HTTP/1.1
Host: direct.example.test
Accept: application/json

HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer realm="direct"
Content-Type: application/json

{"error":"unauthorized"}
```

No `resource_metadata`, issuer, or OAuth authorization-server metadata is advertised. A request to `/.well-known/oauth-protected-resource/mcp` receives `404` with JSON rather than HTML.

### OAuth-only and combined challenge

```http
GET /mcp HTTP/1.1
Host: direct.example.test
Accept: application/json

HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer resource_metadata="https://direct.example.test/.well-known/oauth-protected-resource/mcp"
Content-Type: application/json

{"error":"unauthorized"}
```

The referenced metadata request succeeds in both OAuth-capable profiles:

```http
GET /.well-known/oauth-protected-resource/mcp HTTP/1.1
Host: direct.example.test

HTTP/1.1 200 OK
Content-Type: application/json

{"resource":"https://direct.example.test/mcp","authorization_servers":["https://login.example.test"],"scopes_supported":["direct:read","direct:write"]}
```

The corresponding synthetic authorization request contains `resource=https%3A%2F%2Fdirect.example.test%2Fmcp`, `code_challenge_method=S256`, and an actual S256 challenge. The token request contains the same encoded `resource` and matching verifier. These are required fields, not complete runnable requests.

| Request / configuration | Expected response or result |
| --- | --- |
| PAT-only, no Authorization | 401 JSON; Bearer realm only, no OAuth metadata. |
| PAT-only, valid `PAT_FIXTURE` with required role | Authentication and authorization pass; MCP handler runs. |
| PAT-only, `OAUTH_FIXTURE` | 401 JSON; no OAuth verifier. |
| OAuth-only, no Authorization | 401 JSON with `resource_metadata`; metadata GET returns the document above. |
| OAuth-only, valid `OAUTH_FIXTURE`, correct audience and role | Authentication and authorization pass; MCP handler runs. |
| OAuth-only, `PAT_FIXTURE` | 401 JSON; no PAT dispatch. |
| Combined, valid `PAT_FIXTURE` | Application PAT branch; same product checks. |
| Combined, valid `OAUTH_FIXTURE` | OAuth branch; same product checks. |
| Combined, missing credential | 401 JSON with OAuth resource metadata. |
| Combined, bad-checksum PAT | 401 JSON; no OAuth fallback. |
| Combined, `DPoP` Authorization scheme with a `direct_pat_v1_` value | 401 JSON in PAT branch; no OAuth verification or introspection. |
| OAuth token for Studio presented to Direct | 401 JSON for wrong audience. |
| Resource parameter missing or different at either OAuth request | No usable Direct access token; request refused under this contract. |
| OAuth-only, valid `direct:read` token, allowed role, operation also requires `direct:write` | 403 JSON with the insufficient-scope challenge below; client requests union `direct:read direct:write`, subject to retry limit. |
| Combined, OAuth branch, same missing write scope | Same 403 and challenge; no PAT fallback; bounded union-scope step-up. |
| PKCE metadata absent | Client refuses authorization. |
| S256 capability advertised | Client uses S256. |
| MCP client without DPoP, credential not sender-bound | Not refused solely for lack of DPoP. |
| `cnf.jkt`-bound token without DPoP header | 401; no plain-bearer downgrade. |
| `cnf.jkt`-bound token with wrong proof key thumbprint | 401. |
| `cnf.jkt`-bound token with replayed proof `jti` | 401. |
| `cnf.jkt`-bound token with proof `htu` mismatch | 401. |
| `cnf.jkt`-bound token with proof `ath` mismatch | 401. |
| `cnf.jkt`-bound token with stale proof `iat` | 401. |
| `cnf.jkt`-bound token with proof `htm` mismatch | 401. |
| Protected-resource metadata `resource=https://studio.example.test/mcp` when Direct was requested | Client refuses discovery for resource mismatch. |
| Authorization request uses an unregistered resource indicator | Authorization server refuses; no access token. |
| Authorization request uses `code_challenge_method=plain`, or omits PKCE | Authorization server refuses; no access token. |
| Session cookie plus Authorization bearer | Two credentials; 400 machine-readable refusal before dispatch. |
| AuthProxy identity envelope plus bearer not cryptographically covered by it | Two credentials; 400 machine-readable refusal before dispatch. |
| Verified `oauth-grant` AuthProxy envelope covering the exact OAuth bearer, matching account key, all other checks pass | One credential; caller constructed only through OAuth verification, handler admitted. |
| Combined, verified non-`pat` envelope covering a `direct_pat_v1_` value | 401; refused, no caller constructed. |
| Combined, verified `pat`-kind envelope covering a revoked PAT | 401 after local PAT verification; no caller constructed. |
| Verified envelope covering the exact OAuth bearer but with a different signed subject | 401; refused, no caller constructed. |
| More than one credential, or a credential in the URI query | 400 machine-readable refusal before dispatch. |
| Valid authentication but role denies operation | 403 machine-readable refusal; no scope-based role elevation. |
| Any profile, browser login redirect or HTML error | Nonconforming; expected machine response without `Location`. |

For the two OAuth insufficient-scope vectors, the response is:

```http
HTTP/1.1 403 Forbidden
WWW-Authenticate: Bearer error="insufficient_scope", scope="direct:write", resource_metadata="https://direct.example.test/.well-known/oauth-protected-resource/mcp"
Content-Type: application/json

{"error":"insufficient_scope"}
```

For example, a fixture retry limit of 2 allows no third step-up attempt. This value is a test setting, not a locked production default. A PAT request in the combined profile remains an application PAT flow and MUST NOT be sent through OAuth step-up automatically.

Machine-readable group: [profiles and discovery](../../Source/Identity.Specs/Vectors/arc-mcp-authorization-v1/profiles-and-discovery.json), repository path `Source/Identity.Specs/Vectors/arc-mcp-authorization-v1/profiles-and-discovery.json`. Successful fixture requests assert handler admission, not a particular MCP method's response payload. The group has `name`, `description`, `given`, `when.action`, and `then`; `given.vectors` contains individual cases. These data vectors do not demonstrate runtime conformance. Outcomes and reasons are vector vocabulary, not public error messages.

## Vector artifact

Vector artifact version: 1.0.0

The [manifest](../../Source/Identity.Specs/Vectors/arc-mcp-authorization-v1/manifest.json)
assigns stable case IDs without modifying the signed-off vector bytes. Each case uses
`file` and a zero-based `index` into `given.vectors`. Its `sha256` hashes the UTF-8
compact JSON object (property order preserved, no whitespace or trailing newline,
unescaped Unicode, as produced by JavaScript `JSON.stringify`). File hashes cover the
original bytes. The [changelog](../../Source/Identity.Specs/Vectors/arc-mcp-authorization-v1/CHANGELOG.md)
versions the artifact independently of contract version 1.

The independently reviewed [v1.0.0 baseline](../../Source/Identity.Specs/Vectors/arc-mcp-authorization-v1/baselines/v1.0.0.json)
locks the case-ID-to-content-hash bindings and vector-file hashes. Specs and packaging
compare the manifest against `baselines/v<version>.json`, not against values regenerated
from the manifest. Released baselines are immutable: never edit or regenerate them.
Every version bump adds a new independently approved baseline, a changelog section,
and a `## Vector artifact` section with its own `Vector artifact version:` metadata;
retain older baselines and both versioned sections byte-identically. The checks require one
baseline per changelog heading, including the initial release. The release tag
`vectors/arc-mcp-authorization/v<version>` is the trust anchor, not the mutable working-tree
baseline. `node scripts/check-vector-baseline-immutability.mjs` compares every released
baseline with `git show <tag>:<path>` and, while the manifest selects that version,
compares the manifest and vector content hashes with the tagged revision. It also compares
the tagged version's changelog and vector-artifact sections byte-for-byte. Historical
baselines and sections remain unchanged when the manifest and vectors advance to a new version.
An untagged baseline passes with an unreleased notice; v1.0.0 is anchored when its first
release tag is cut after the next train merges. On pull requests, the checker also compares
against `BASE_REF` (or `--base-ref <ref>`, default `origin/main` for pull requests): old
baselines and versioned documentation sections must remain byte-identical, and changing
the manifest, vectors, or versioned prose requires a version bump with a new baseline
and new documentation sections. Build CI and the packaging workflow run this check
with full Git history and tags. Run `node scripts/check-vector-baseline-immutability.mjs
--self-test` to exercise the rejection rules in disposable Git repositories.
IDs are never reassigned to different behavior. Changing a case requires a new ID or an
artifact version bump; even with a bump, an existing ID must not acquire different meaning.
Any ID or hash change at the current version fails baseline validation.

### Packaging and manual publication

Run `node scripts/check-vector-baseline-immutability.mjs` before
`node scripts/package-authorization-vectors.mjs .ai-work/identity-134/artifacts`
from the repository root. This Node-stdlib script produces
`arc-mcp-authorization-v1-vectors-1.0.0.tar.gz` and its `.sha256` sidecar. The archive
contains `manifest.json`, `CHANGELOG.md`, every listed vector file at its root, and all
versioned baselines under `baselines/`. Packaging validates the current baseline and
baseline/changelog history before writing the archive.
Entries are sorted with fixed mode, owner, and timestamp fields; gzip timestamps and
OS metadata are normalized. Use the same Node version for byte-identical compression.

The release owner can run the **Package authorization vectors** `workflow_dispatch`
workflow on the reviewed commit to download the pair as a workflow artifact. It does
not create a Git tag or GitHub release and never runs on push. Publication of the pair
as immutable assets under the separately approved trust-anchor tag
`vectors/arc-mcp-authorization/v1.0.0` remains a release-owner action. No cross-repository
dispatch or token is used.

Arc and AuthProxy consumers should pin the published artifact version and SHA-256,
verify the sidecar before extraction, verify the manifest and case coverage, and run
their own conformance specs by case ID. A missing, duplicate, or unexecuted case must
fail their gate. Consumer CI wiring and local overrides are separate deliverables;
this package does not assert consumer conformance.

## Decisions recorded at sign-off

Decided in Identity #14 (2026-11-05) with the recommended answers from the sign-off package:

- **Cratis/Identity#34:** the 2026-07-28 pin is confirmed. The supported versions and the finite production step-up retry limit are decided at sign-off. Verifying external connector behavior, including DPoP support, is implementation evidence and remains unverified.
- **D9:** registration mechanisms follow tested connector needs; the independent registration security contract remains required.
- **D11 / q11:** PAT prefix and checksum encoding follow [arc-pat-format-v1](arc-pat-format-v1.md); no fallback on invalid PATs is part of this contract.
- **q9 / N1:** production resource indicators, audiences, issuer names, and scope names are decided at sign-off. Test URLs are not production registrations.
- **(align with #24):** token type, single catalog audience string, `tid`, assurance, and caller mappings follow #24. This spec needs additional profile/discovery behavior, PKCE metadata handling, registration draft pins, and scope-step-up retry policy; #24 does not define those protocol mechanics. No additional environment claim or arbitrary token claims are introduced.

## Versioning

This spec MUST change only through a new version file. Changing the pinned MCP revision MUST receive explicit compatibility review and connector tests. Adding OAuth to a PAT-only deployment MUST be an explicit profile change; it MUST NOT silently invalidate active PATs outside their separately announced lifecycle.

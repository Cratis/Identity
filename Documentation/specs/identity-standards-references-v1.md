# Verified standards references

```text
Spec id: identity-standards-references
Version: 1
Status: Signed off
Signed off: 2026-11-05 by woksin, https://github.com/Cratis/Identity/issues/14; owner confirmation pending
Issue: https://github.com/Cratis/Identity/issues/34
Sign-off issue: https://github.com/Cratis/Identity/issues/14
Supersedes: None
```

## 1. Scope and verification

Verified **2026-11-05**. This records the checked references underlying the Identity/Arc contracts; it is not protocol conformance evidence or tested connector compatibility. Keywords follow [the specification conventions](README.md). CONFIRMED means the relied-on statement was confirmed, not that the implementation passes; CORRECTED identifies wording/revision changes. Drafts are not RFCs. Client documentation is capability evidence at the verification date, not an interoperability guarantee.

## 2. Standards reference table

| Standard | Status | What we rely on | Verdict | Corrected statement | URL |
|---|---|---|---|---|---|
| OpenID Connect Core 1.0, §§2, 5.7 | Final specification | `sub` is issuer-local, never reassigned, case-sensitive and at most 255 ASCII characters; only the `iss` + `sub` combination provides the stable end-user identifier guarantee | CONFIRMED | Key accounts by exact issuer and subject, not email, display name or a globally unique `sub` assumption. Our UUID subject shape is a local profile, not an OIDC requirement. | [OIDC Core](https://openid.net/specs/openid-connect-core-1_0.html) |
| RFC 9068 | Standards Track | JWT access-token profile: required `iss`, `exp`, `aud`, `sub`, `client_id`, `iat`, `jti`; explicit access-token typing | CONFIRMED | Do not assume library defaults alone meet our profile. Check actual issued claims, audience and signed-only token format; OpenIddict behavior is recorded separately below. | [RFC 9068](https://www.rfc-editor.org/rfc/rfc9068) |
| RFC 8693, §§4.1, 4.4 | Standards Track | `act` identifies the current actor; nested actors describe prior actors; `may_act` expresses authorization to become an actor | CONFIRMED | Only the outermost actor participates in access control; nested history is informational, not delegated authority. `may_act` is not the current actor. | [RFC 8693](https://www.rfc-editor.org/rfc/rfc8693) |
| RFC 9728 | Standards Track, April 2025 | OAuth protected-resource metadata and the `WWW-Authenticate` `resource_metadata` parameter | CONFIRMED | Discover via `/.well-known/oauth-protected-resource`, inserting the well-known segment between host and resource path for path-based resources; challenge metadata identifies the protected resource, not issuer metadata. | [RFC 9728](https://www.rfc-editor.org/rfc/rfc9728) |
| RFC 8252 | Best Current Practice (BCP 212) | Native-app loopback redirects and PKCE for public native clients | CONFIRMED | Use loopback IP literals `127.0.0.1` / `[::1]`; allow any request-time port for loopback IP redirects. `localhost` is not recommended. Public native clients must use PKCE. | [RFC 8252](https://www.rfc-editor.org/rfc/rfc8252) |
| RFC 8707 | Standards Track | `resource` identifies the intended protected resource | CONFIRMED | A resource indicator is an absolute URI with no fragment. Resource registration maps it to the exact audience; do not equate client id with resource audience. | [RFC 8707](https://www.rfc-editor.org/rfc/rfc8707) |
| RFC 9449 | Standards Track | DPoP binds tokens to a public-key thumbprint and requires request-specific proof validation | CONFIRMED | Proofs require `jti`, `htm`, `htu` (no query/fragment), `iat`, plus `ath` when presenting an access token and a server nonce when required via `DPoP-Nonce`. `cnf.jkt` is the SHA-256 JWK thumbprint. Optional client support never permits a bound token to downgrade to Bearer. | [RFC 9449](https://www.rfc-editor.org/rfc/rfc9449) |
| RFC 6750 | Standards Track | Bearer token transport and authentication challenges | CONFIRMED | Use TLS and the Authorization header; do not send a token by multiple transport methods in one request. Our contracts additionally refuse URI-query credentials rather than treating the RFC's query method as recommended practice. DPoP-bound tokens use the separate DPoP profile. | [RFC 6750](https://www.rfc-editor.org/rfc/rfc6750) |
| RFC 7643 / RFC 7644 | Standards Track | SCIM core schema / HTTP protocol | CONFIRMED | RFC 7643 defines the schema; RFC 7644 defines the protocol. Provisioning is not product-owned live authorization or proof of current membership. | [RFC 7643](https://www.rfc-editor.org/rfc/rfc7643), [RFC 7644](https://www.rfc-editor.org/rfc/rfc7644) |
| OAuth 2.1, draft-ietf-oauth-v2-1-17 | Internet-Draft, 31 October 2026; not an RFC | Current OAuth 2.1 draft at verification; secure authorization-code/PKCE baseline | CORRECTED | Refer to draft -17 as the current draft, not a published OAuth 2.1 RFC. The pinned MCP revision references draft -13; that citation does not silently upgrade to -17. | [Draft -17](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-v2-1-17), [MCP-cited draft -13](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-v2-1-13) |
| CIMD, draft-ietf-oauth-client-id-metadata-document-03 | OAuth WG Internet-Draft, 2 November 2026; Standards Track intent, not an RFC | Client ID Metadata Documents for client metadata discovery | CORRECTED | The checked identifier is `draft-ietf-oauth-client-id-metadata-document-03`; do not describe CIMD as an RFC or confuse it with DCR. | [CIMD draft -03](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-client-id-metadata-document-03) |
| MCP authorization 2026-07-28 | Dated MCP specification revision, not an IETF RFC | RFC 9728 server/client discovery; resource binding; PKCE; client metadata and scoped challenges | CORRECTED | Servers must implement and clients must use protected-resource metadata; `resource` is required in authorization and token requests; validate tokens as issued for this server. Use PKCE S256 when capable and refuse if required capability metadata is missing. CIMD is SHOULD; DCR is MAY and deprecated. `insufficient_scope` uses 403 with `scope` and `resource_metadata`; clients should step up with the union of scopes and bounded retries. | [Authorization 2026-07-28](https://modelcontextprotocol.io/specification/2026-07-28/basic/authorization), [security considerations](https://modelcontextprotocol.io/specification/2026-07-28/basic/security-considerations) |

### OpenIddict implementation reference

| Implementation | Status | What we rely on | Verdict | Corrected statement | URL |
|---|---|---|---|---|---|
| OpenIddict 7.8.0 | Library release; not a standard | Access-token `typ` is `at+jwt`; default access-token encryption; `client_id` added if available; `jti` generated as a Guid; `aud` derived from principal resources | CORRECTED | Call `DisableAccessTokenEncryption()` for external signed-only JWT consumers, require `client_id` on every supported issuance path, map registered resources to the exact audience, and strip private `oi_` claims before serialization. Verify final output, not just option setters; these defaults alone do not prove RFC 9068/local-profile conformance. | [Token formats](https://documentation.openiddict.com/configuration/token-formats), [7.8.0 server handlers](https://github.com/openiddict/openiddict-core/blob/7.8.0/src/OpenIddict.Server/OpenIddictServerHandlers.cs), [7.8.0 protection handlers](https://github.com/openiddict/openiddict-core/blob/7.8.0/src/OpenIddict.Server/OpenIddictServerHandlers.Protection.cs) |

## 3. Pinned revisions

MCP authorization is pinned to **2026-07-28**, using the [dated authorization URL](https://modelcontextprotocol.io/specification/2026-07-28/basic/authorization) and its [dated security considerations](https://modelcontextprotocol.io/specification/2026-07-28/basic/security-considerations). The [MCP versioning page](https://modelcontextprotocol.io/specification/versioning) identified this as the current revision when verified on 2026-11-05. No moving latest or draft URL is the contract reference.

A newer MCP revision MUST be adopted only through a **new version of this spec**, with review of dependent contracts. A versioning-page change does not update this pin. [arc-mcp-authorization-v1](arc-mcp-authorization-v1.md) uses this revision, including its OAuth 2.1 draft -13 reference. The current OAuth 2.1 draft -17 and CIMD draft -03 above are separately identified research baselines, not a claim that MCP's dated bibliography changed.

## 4. Client capability matrix

Capabilities below are documented as of 2026-11-05, not executed tests. Claude Code and claude.ai share authorization infrastructure. DCR support does not mean it must be enabled on our server; the [MCP profile](arc-mcp-authorization-v1.md) controls registration policy.

| Client | PKCE S256 | CIMD | DCR | DPoP | Evidence |
|---|---|---|---|---|---|
| Claude Code / claude.ai | Documented; always used | Documented; preferred | Documented fallback | **UNVERIFIED — no evidence found, not evidence of non-support** | [Claude connector authentication](https://claude.com/docs/connectors/building/authentication) |
| Codex | Documented | Documented | Documented | **UNVERIFIED — no evidence found, not evidence of non-support** | [Codex MCP](https://developers.openai.com/codex/mcp), [OpenAI authentication](https://developers.openai.com/plugins/build/auth) |
| ChatGPT | Documented | Documented; preferred | Documented | **UNVERIFIED — no evidence found, not evidence of non-support** | [OpenAI authentication](https://developers.openai.com/plugins/build/auth) |

Do not require DPoP merely because a client speaks MCP. Follow [identity-dpop-v1](identity-dpop-v1.md): DPoP is optional for eligible MCP clients with unbound tokens, subject to the profile's registration and organisation policy; a `cnf.jkt`-bound token always requires valid DPoP and never downgrades to Bearer. Capability documentation does not prove our discovery, consent, step-up, redirect or token paths interoperate; conformance remains separate work.

## 5. Corrections applied

1. Designs cited MCP **2025-11-25**; the verified current pin is **2026-07-28**, used by [arc-mcp-authorization-v1](arc-mcp-authorization-v1.md). This document records the correction; it does not rewrite the design documents or claim a GitHub issue was updated.
2. DPoP is optional for eligible MCP clients, not a universal connector prerequisite; this is reflected in [identity-dpop-v1](identity-dpop-v1.md). Bound-token enforcement and organisation policy remain mandatory as defined there.
3. OpenIddict access-token encryption must be disabled and private `oi_` claims stripped for the external JWT profile; both requirements appear in [identity-access-token-v1](identity-access-token-v1.md). That specification requires actual serialized-output conformance, not merely configuration changes.

## 6. Acceptance and remaining evidence

- Historical-revision scan: every existing spec under `Documentation/specs/` was checked with `rg` on 2026-11-05. The only remaining old-date occurrence is this file's §5 item 1, intentionally retained to explain the correction, **not** an active pin. No other spec occurrence remains. Keep this traceability occurrence visible during review rather than silently treating it as a current dependency.
- `arc-mcp-authorization-v1.md` records its revision pin as confirmed in Identity #34. This references spec supplies the dated findings; connector verification under #34 / Arc #3703 is implementation evidence; sign-off was recorded in #14 (2026-11-05). No other contract was edited in this change.
- DPoP client support remains unverified. Authoritative capability evidence or compatibility tests are required before changing client policy; absence of documentation is not non-support.
- Review of OpenIddict handler ordering/output and the revocation/DPoP contracts with their owners was signed off in Identity #14 (2026-11-05). References verification is not release approval, a completed implementation, or an OpenIddict/connector conformance result.

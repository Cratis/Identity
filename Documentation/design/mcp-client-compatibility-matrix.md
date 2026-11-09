# MCP client compatibility matrix

```text
Doc id: mcp-client-compatibility-matrix
Version: 1
Status: Draft
Checked: 2026-11-09 against vendor documentation (sources per client below)
```

This note records how each MCP client that Identity must serve registers itself, what it reads during discovery, and what its redirect URI looks like. It is built from vendor documentation and the MCP authorization specification. **Nothing here was tested against a running client**, so every row is "documented", never "tested". Where a vendor page is silent, the cell says `UNVERIFIED`. That is a statement about the documentation, not a claim the client lacks the behavior.

The MCP authorization specification version 2026-07-28 (the current release) governs: https://modelcontextprotocol.io/specification/2026-07-28/basic/authorization

## Registration approaches in one paragraph

An MCP client can identify itself to an authorization server in three ways. **Pre-registered** means a human creates a client ID (and optionally a secret) in advance and gives it to the client. **Client ID Metadata Document (CIMD)** means the client uses an HTTPS URL as its `client_id`, and the authorization server fetches the JSON document at that URL to learn the client's name and redirect URIs. **Dynamic Client Registration (DCR)** means the client calls a `registration_endpoint` and receives a new client ID. The specification says clients and servers SHOULD support CIMD, MAY support DCR, and describes DCR as retained for backwards compatibility. A client that finds `client_id_metadata_document_supported` in authorization server metadata prefers CIMD, then falls back to DCR when `registration_endpoint` is present.

## Matrix

| Client | Registration it uses | Redirect URI shape | Public or confidential |
|---|---|---|---|
| Claude Code | Pre-registered through `--client-id` (and `--client-secret` only for a confidential client); CIMD is used when discovered; DCR is the default when the server offers it | Loopback on an ephemeral port, `http://localhost:PORT/callback`; `--callback-port` fixes the port. CIMD document declares `http://localhost/callback` and `http://127.0.0.1/callback` | Public by default; confidential only if a secret is supplied |
| Codex | Pre-registered through `--oauth-client-id`; CIMD and DCR both documented | `http://127.0.0.1/callback` with the active listener port inserted at login (variable loopback port). Without issuer identification support, a server-specific callback ID is appended: `http://127.0.0.1/callback/<id>` | Public by default; confidential with `--oauth-client-secret` |
| Cursor | Static client through `auth.CLIENT_ID` in `mcp.json`; DCR when none is configured. CIMD `UNVERIFIED` | Fixed: `http://localhost:8787/callback` (desktop) and `https://www.cursor.com/agents/mcp/oauth/callback` (web and cloud agents) | Public with DCR; confidential if `CLIENT_SECRET` is set |
| VS Code | DCR first; if the server has none, a client-credentials fallback in which the user supplies a client ID and optional secret (this is a pre-registered client). CIMD `UNVERIFIED` | Authorization server must allow `http://127.0.0.1:33418` and `https://vscode.dev/redirect` | `UNVERIFIED` for DCR; confidential if the user enters a secret |
| claude.ai connector | CIMD when the authorization server advertises both `client_id_metadata_document_supported: true` and `none` in `token_endpoint_auth_methods_supported`; otherwise DCR. Also supports Anthropic-held client credentials and a customer-entered client | Fixed hosted callback `https://claude.ai/api/mcp/auth_callback` (shared by web, Desktop, mobile) | Public under CIMD (`none`) and DCR; a second `confidential_client` CIMD document with `private_key_jwt` exists for some servers; confidential when a secret is entered |
| ChatGPT connector | CIMD preferred when advertised, with the stable client ID `https://chatgpt.com/oauth/client.json`; DCR if `registration_endpoint` exists and the plugin builder chooses it; predefined client also supported | Stable `https://chatgpt.com/connector_platform_oauth_redirect` when issuer identification is advertised, otherwise `https://chatgpt.com/connector/oauth/{callback_id}` | Public (`none`) or confidential (`private_key_jwt` against a published JWKS) under CIMD; DCR and predefined clients commonly `none`, `client_secret_post` or `client_secret_basic` |
| MCP Inspector (developer tool) | DCR, pre-registered (`--client-id`), or CIMD (`--client-metadata-url`) | Web: `http://127.0.0.1:6274/oauth/callback` by default (follows the page origin); CLI and TUI: `http://127.0.0.1:6276/oauth/callback` | Public unless a secret is given |

### Discovery, PKCE, resource and token behavior

| Client | Protected-resource metadata (RFC 9728) | Authorization server metadata | PKCE and `resource` | Refresh and lifetime |
|---|---|---|---|---|
| Claude Code | Follows `WWW-Authenticate` on 401/403 to the authorization server; discovery is automatic | Needs it to detect CIMD or DCR support; rejects an authorization response whose `iss` names an unexpected issuer | S256 and `resource`: `UNVERIFIED` in vendor docs (the specification requires both) | Refreshes automatically; retries once on 401; no refresh token means it signs in again after expiry; does not revoke at the authorization server on removal |
| Codex | Reads it (OAuth discovery documented); path-suffix form `UNVERIFIED` | Uses `authorization_response_iss_parameter_supported` and the metadata `issuer` to choose a stable callback; requires variable loopback ports under RFC 8252 section 7.3 | S256 and `resource`: `UNVERIFIED` in vendor docs | Stored OAuth tokens; refresh behavior `UNVERIFIED`. A 403 insufficient-scope response does not trigger a login |
| Cursor | Reads it to find the authorization server and the resource indicator | Uses `scopes_supported` from `/.well-known/oauth-authorization-server` when no scopes are configured | Authorization code with PKCE; sends the resource indicator it finds in protected-resource metadata | Renews with the refresh token; with `expires_in` and no refresh token it signs in again; a refresh token may require `offline_access` in `auth.scopes` |
| VS Code | Follows the MCP authorization specification; specifics `UNVERIFIED` | `UNVERIFIED` | `UNVERIFIED` in vendor docs | `UNVERIFIED` |
| claude.ai connector | Needs a 401 with `resource_metadata` (a 200 with the header is ignored); otherwise probes `/.well-known/oauth-protected-resource/<mcp-path>` then `/.well-known/oauth-protected-resource`. The document's `resource` must equal the URL the user entered | Needs `code_challenge_methods_supported` with `S256`; reads `scopes_supported` and appends `offline_access` when listed | S256 on every request. `resource` presence is not stated on the vendor page (the document's `resource` must match the entered URL) | Refreshes reactively on 401 and up to five minutes before expiry; wants `invalid_grant` for dead refresh tokens; expects rotation for public clients; accepts form-encoded token requests; 10 second limit on discovery, registration and token endpoints, 30 seconds for refresh |
| ChatGPT connector | `GET /.well-known/oauth-protected-resource` or `WWW-Authenticate: Bearer resource_metadata=...` | Needs `authorization_endpoint`, `token_endpoint`, `code_challenge_methods_supported` including `S256`; reads `client_id_metadata_document_supported`, `registration_endpoint`, `token_endpoint_auth_methods_supported`; requests OIDC scopes the server advertises; issuer identification (`iss`, RFC 9207) gives the stable redirect URI | S256 PKCE; appends `resource` to both authorization and token requests | Refresh behavior and token lifetime not stated on the cited page: `UNVERIFIED` |
| MCP Inspector | Per the specification | Per the specification | `UNVERIFIED` (developer tool, not a launch target) | `UNVERIFIED` |

DPoP (RFC 9449) support is `UNVERIFIED` for every client above; none of the cited pages mention it. Identity must not refuse a client solely because it does not send DPoP.

### Sources

Each claim above comes from the page for its row, read on 2026-11-09. Versions were not stated on the pages; record the client version when each is tested.

| Client | Source |
|---|---|
| Claude Code | https://code.claude.com/docs/en/mcp (sections on OAuth, fixed callback port, pre-configured credentials) and https://claude.com/docs/connectors/building/authentication (Claude Code CIMD and loopback redirect) |
| Codex | https://developers.openai.com/codex/mcp (OAuth client registration and callbacks) |
| Cursor | https://cursor.com/docs/context/mcp (static OAuth, static redirect URL, authentication) |
| VS Code | https://code.visualstudio.com/api/extension-guides/ai/mcp (Authorization) |
| claude.ai connector | https://claude.com/docs/connectors/building/authentication |
| ChatGPT connector | https://developers.openai.com/apps-sdk/build/auth |
| MCP Inspector | https://modelcontextprotocol.io/docs/tools/inspector/authorization |
| Specification | https://modelcontextprotocol.io/specification/2026-07-28/basic/authorization |

## CIMD draft version

The MCP specification page for 2026-07-28 cites `draft-ietf-oauth-client-id-metadata-document-00`, as does 2025-11-25. The MCP draft page cites `-03`. The IETF datatracker lists versions 00 to 03 and shows `-03` as the active draft, last updated 2026-11-02, expiring 6 May 2027 (https://datatracker.ietf.org/doc/draft-ietf-oauth-client-id-metadata-document/).

Identity pins `-03`. Changes from `-00` that affect an authorization server:

- The document `client_id` must equal the fetched URL by simple string comparison, and the server must not normalize it. Clients should normalize their own URL.
- The root path `/` is not allowed as a Client Identifier URL.
- A loopback redirect exception follows RFC 9700.
- A document with neither `token_endpoint_auth_method` nor `token_endpoint_auth_methods_supported` means `none`; the plural field is preferred and the singular is not recommended, but the server must still handle both. ChatGPT already publishes both during the transition.
- Fetch failures are classified as transient or terminal, and the server may rate limit fetches to a Client Identifier URL.
- Secrets and symmetric keys are not allowed in a document; `private_key_jwt` requires `jwks` or `jwks_uri`.

Clients in the wild: ChatGPT documents the plural-and-singular transition; Claude documents `none` and `private_key_jwt` documents. Whether any shipping client sends a pure `-00` document is `UNVERIFIED`; a `-00` document that carries only the singular field and valid redirect URIs is still readable under `-03`, so pinning `-03` and accepting the singular field covers both.

**Re-check trigger:** repeat this comparison when the MCP specification changes the draft it cites, or when the IETF draft reaches a new version or is published as an RFC.

## Is Dynamic Client Registration needed on day one?

**No, not for the day-one client set**, provided Identity serves CIMD and publishes the metadata the connectors check:

- Claude Code, Codex, Cursor and VS Code work with a pre-registered client. Claude Code, Codex and Cursor document an explicit client ID option; VS Code documents a client-credentials fallback when DCR is absent.
- claude.ai selects CIMD only when the authorization server advertises both `client_id_metadata_document_supported: true` and `none` in `token_endpoint_auth_methods_supported`; otherwise it falls back to DCR. If Identity advertises both values, claude.ai needs no DCR. Anthropic-held credentials and a customer-entered client are documented alternatives.
- ChatGPT prefers CIMD when `client_id_metadata_document_supported` is true.

The conclusion depends on those metadata values being served and on the documented behavior holding in a real run, which has not been tested. If a hosted connector is later found to require `registration_endpoint`, DCR would need its own registration security contract (public clients, exact redirect URIs, rate limits, bounded lifetime).

## What Identity must provide

These become acceptance checks for client support:

1. Protected-resource metadata is served on a 401 `WWW-Authenticate: Bearer resource_metadata=...` and at both `/.well-known/oauth-protected-resource` and the path-suffix form `/.well-known/oauth-protected-resource/mcp`; `resource` equals the URL users enter, including the path.
2. Authorization server metadata advertises `code_challenge_methods_supported: ["S256"]`, `client_id_metadata_document_supported: true`, `token_endpoint_auth_methods_supported` including `none`, `authorization_response_iss_parameter_supported: true`, and `scopes_supported` (including `offline_access` if refresh tokens are wanted).
3. Every authorization response, success and error, carries `iss` matching the metadata `issuer` exactly. Codex and ChatGPT use it to select a stable redirect URI, and Claude Code rejects an unexpected issuer.
4. `resource` is required on both authorization and token requests, and the token carries the bound audience.
5. The token endpoint accepts `application/x-www-form-urlencoded`, returns `invalid_grant` for a dead refresh token, rotates refresh tokens for public clients, and answers well inside 10 seconds (30 seconds for refresh).
6. CIMD fetch: HTTPS only, no redirects, `client_id` equals the fetched URL, size and time limits, SSRF protection, caching that honors HTTP cache headers, redirect URIs validated against the document, no secrets in documents, `-03` rules above. Hosted client documents to expect: `https://chatgpt.com/oauth/client.json` and its callback-ID form `https://chatgpt.com/oauth/{callback_id}/client.json`; the claude.ai document URL is `UNVERIFIED` (the cited page does not state it).
7. Redirect URIs: hosted callbacks `https://claude.ai/api/mcp/auth_callback`, `https://chatgpt.com/connector_platform_oauth_redirect` and the callback-ID form `https://chatgpt.com/connector/oauth/{callback_id}`; pre-registered CLI clients need a variable port on `127.0.0.1` (Codex, Inspector) and the fixed `http://127.0.0.1:33418` and `https://vscode.dev/redirect` (VS Code).
8. A 403 `insufficient_scope` challenge is understood by clients for step-up; vendor pages confirm step-up behavior for none of them except the specification, so verify per client when testing.
9. A consent screen that displays the redirect URI hostname, because a loopback-only registration can be claimed by any local process.

### Conflict to resolve before relying on loopback clients

Claude Code (CIMD document and the `--callback-port` form), Cursor (`http://localhost:8787/callback`) document redirect URIs that use the host name `localhost`. RFC 8252 section 8.3 discourages `localhost`, and Identity's supported profile accepts the variable-port exception only for IP literals. A pre-registered Claude Code or Cursor client that must match the documented `localhost` redirect is refused under that profile unless the clients are configured to use `127.0.0.1`, or the profile is relaxed for registered clients. Claude Code version 2.1.229 sent `127.0.0.1`, and 2.1.231 restored `localhost`, so this behavior has changed between releases. This is a policy decision, not resolved here.

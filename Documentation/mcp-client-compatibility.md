# MCP client compatibility matrix

Which remote MCP clients can sign in against Cratis Identity, and what the authorization server must therefore support. Facts come from vendor documentation and the MCP authorization specification; anything not confirmed is marked UNVERIFIED and must be proven against the real client in the deployed run before it is relied on.

## Specification baseline

- The 2025-11-25 revision of the MCP authorization specification makes Client ID Metadata Documents (CIMD) the preferred registration method, with Dynamic Client Registration (DCR) optional. The 2026-07-28 revision ranks CIMD above DCR and marks DCR deprecated.
- Clients must use PKCE with the S256 method and must send the `resource` parameter (RFC 8707). Resource servers publish protected resource metadata (RFC 9728).
- DPoP is an optional extension proposal, not part of the core specification.

## Matrix

| Client | Registration | Redirect URI | Bearer header fallback | DPoP |
|---|---|---|---|---|
| claude.ai connectors | CIMD, DCR or vendor-held credentials | Fixed hosted callback | Yes (beta static headers) | UNVERIFIED |
| ChatGPT | CIMD preferred, DCR or predefined client | Fixed hosted callbacks | UNVERIFIED | UNVERIFIED |
| Claude Code | CIMD (auto-discovered), DCR or pre-set client id | Loopback with random port, port can be fixed | Yes | UNVERIFIED |
| Codex CLI | CIMD when advertised, else DCR; client id override | Loopback with ephemeral port, port can be fixed | Yes | UNVERIFIED |
| Cursor | Pre-set client id where DCR is absent, else DCR; CIMD UNVERIFIED | Fixed hosted callback and a fixed loopback port | Yes | UNVERIFIED |
| VS Code | DCR first, then client credentials prompt; CIMD UNVERIFIED | Fixed loopback port and a hosted redirect | Yes (provider specific) | UNVERIFIED |

PKCE S256 and RFC 8707 handling are documented for ChatGPT and required by the specification for all clients; per-client behavior for Claude Code, Codex, Cursor and VS Code is UNVERIFIED and is covered by the deployed run.

## Consequences for Identity

1. CIMD is the primary registration path: it covers claude.ai, ChatGPT, Claude Code and Codex. Its fetcher must be SSRF-guarded and redirect-safe, and it must not rely on mutable metadata.
2. Cursor and VS Code need either pre-registered clients or narrow DCR. DCR is added only if the deployed run proves a launch client cannot be served by pre-registration.
3. Redirect validation must accept fixed hosted callbacks by exact match and loopback redirects by host and path with any port, only for clients that registered a loopback redirect.
4. No documented client supports DPoP, so DPoP cannot be required on the MCP path. Bearer access tokens, audience-bound and short-lived, are the baseline; DPoP stays optional.
5. Static bearer headers are supported by several clients, which is the basis for the interim product-and-tenant scoped token. A failed OAuth flow never falls back to it automatically.
6. Access tokens only; each product has its own audience and resource indicator.

## Sources

- https://modelcontextprotocol.io/specification/2025-11-25/basic/authorization
- https://modelcontextprotocol.io/specification/latest/basic/authorization
- https://claude.com/docs/connectors/building/authentication
- https://developers.openai.com/plugins/build/auth
- https://code.claude.com/docs/en/mcp
- https://developers.openai.com/codex/mcp
- https://cursor.com/docs/context/mcp
- https://code.visualstudio.com/api/extension-guides/ai/mcp
- https://docs.github.com/en/copilot/how-tos/copilot-in-your-ide/customize-copilot/extend-copilot-with-tools-and-context/set-up-the-github-mcp-server

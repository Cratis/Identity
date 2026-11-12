# Sign in local MCP clients for Direct and Studio

Sign in once with Identity running on your machine, then let a local MCP client such as Claude, pi or VS Code call both a local Direct MCP server and a local Studio MCP server.

## Before you start

- Identity runs with the local development profile. See [Local development profile](local-development.md) for the GitHub OAuth app, the launch profile and the key store.
- Direct and Studio run locally with their MCP endpoints at the resource indicators in the profile: `http://localhost:5200/mcp` for Direct and `http://localhost:5300/mcp` for Studio. The ports are placeholders; change the profile's indicators if yours differ.

## How the audiences fit together

One sign-in at Identity covers both products. Each access token is issued for one product and carries that product's audience.

| Product | MCP audience | REST audience |
| --- | --- | --- |
| Direct | `direct-mcp` | `direct-api` |
| Studio | `studio-mcp` | `studio-api` |

The two audiences of a product are not interchangeable. An MCP server accepts only its `-mcp` audience, and a REST API accepts only its `-api` audience. A token for one is refused at the other.

The local profile registers only the MCP audiences. Local REST calls to Direct and Studio keep their existing configuration with `direct-api` and `studio-api`.

## Point each product at the local Identity

Configure each product's MCP resource server for the local issuer:

- Issuer: `http://localhost:5100/`
- Signing keys: the JWKS from the authorization server metadata at `http://localhost:5100/.well-known/oauth-authorization-server` (use `/.well-known/openid-configuration` only when OpenID Connect issuance is enabled)
- Audience: `direct-mcp` for Direct, `studio-mcp` for Studio

Leave the REST audience of each product unchanged. Do not set `direct-api` on the MCP resource server.

## Sign in from the client

1. Start Identity with the local development launch profile.
2. Start Direct and Studio.
3. Add each MCP endpoint to your client, using the client id registered for your client in [Clients](local-development.md#clients): `local-claude`, `local-pi` or `local-vscode`.
4. When the client opens the browser, sign in with GitHub and approve the consent screen for the product's scopes.

The client asks for the product's resource indicator, receives an access token for that product only, and sends it to the MCP endpoint. To use the second product, the client repeats the flow for it and you approve consent for that product's scopes.

## Check it works

Call a read tool on each server from the client. A tool call that succeeds on Direct and on Studio confirms the two audiences.

If a call fails with an invalid token or audience error:

- Confirm the product expects the audience in the table, not the REST one.
- Confirm the product validates against `http://localhost:5100/` and not a deployed issuer. A deployed issuer rejects a local token.
- Confirm the client requested the resource indicator of the product it is calling.

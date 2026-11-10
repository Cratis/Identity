# Local development profile

Run Identity on your machine so a local tool, such as Claude, pi or VS Code, can sign in and call a local Studio or Direct MCP server. The profile is opt-in, runs only in the `Development` environment, and issues tokens that no deployed service accepts.

## What it is

When the profile is on, Identity uses a loopback issuer, its own signing and encryption keys, a set of local resources and a few pre-registered public clients. The tools sign in with the authorization code flow and PKCE, as `cratis-cli` does, and receive an access token for one product. They never receive an ID token.

## What it is not

The profile is not a way to reach a deployed environment. It has no deployed keys, no deployed issuer and no deployed resource indicators, and it never merges with deployed configuration.

## Prerequisites

- A running Chronicle server. The default host settings use `chronicle://localhost:35000`.
- The upstream login settings the host already needs.

## Start the host

Start the reference host with the local-development launch profile:

```shell
dotnet run --project Source/Identity.Host --launch-profile "Identity (local development profile)"
```

The profile sets `Identity__Development__Enabled=true` in the `Development` environment. Identity listens on `http://localhost:5100`, and the discovery document is at `http://localhost:5100/.well-known/openid-configuration`.

The flag belongs only in the launch profile or in `appsettings.Development.json`. Setting `Identity:Development:Enabled` in any other environment stops the host at startup with a message that names the setting.

## Keys

The profile creates its keys on first run in a per-user directory outside the repository:

| System | Location |
| --- | --- |
| macOS and Linux | `~/.local/share/cratis/identity-dev/<issuer-authority>/` |
| Windows | `%LOCALAPPDATA%\cratis\identity-dev\<issuer-authority>\` |

The directory is owner-only and holds a marker file, development certificates named `NOT-FOR-DEPLOYMENT`, and the Data Protection ring. Identity refuses to start when the directory holds anything else, is a link, is inside the repository, or is readable by other users. Delete the directory to start over. Never copy, commit or deploy these keys.

New keys are added when the current ones are about to expire. Old ones stay so earlier tokens still validate.

## Local resources and audiences

The settings in `Source/Identity.Host/appsettings.Development.json` replace the deployed resources. They are never merged.

| Product | Audience | Resource indicator | Scopes |
| --- | --- | --- | --- |
| Direct MCP | `direct-api` | `http://localhost:5200/mcp` | `direct:mcp:read`, `direct:mcp:content.write`, `direct:mcp:work` |
| Studio MCP | `studio-mcp` | `http://localhost:5300/mcp` | `studio:mcp:read`, `studio:mcp:write` |

The audience `direct-api` is the existing name for the Direct MCP resource, and the local profile keeps it so a product validates the same audience everywhere. The ports are placeholders. Change the indicators to match how each product runs locally.

## Clients

| Client id | For | Redirect URIs |
| --- | --- | --- |
| `local-claude` | Claude | `http://localhost/callback`, `http://127.0.0.1/callback`, `http://[::1]/callback` |
| `local-pi` | pi (provisional) | the same |
| `local-vscode` | VS Code | `http://127.0.0.1:33418`, `http://127.0.0.1/callback`, `http://localhost/callback` |

Each client is public, requires PKCE and consent, and accepts any port on its loopback redirect URIs. Only these clients may use `localhost`; `cratis-cli` and every deployed client keep `127.0.0.1` and `[::1]`. Add your own entries under `Identity:Development:Clients`.

## Point a product at it

Configure the product's MCP resource server for the local issuer:

- Issuer: `http://localhost:5100/`
- Signing keys: the JWKS from the discovery document
- Audience: the audience in the table above

A product configured for the deployed issuer rejects a local token because the issuer and the keys differ.

## Refusals

Identity stops at startup, naming the setting, when the profile is on and:

- the environment is not `Development`;
- `Identity:Issuer` is not an `http` loopback origin;
- `Identity:SecretStore:Path` or the hosted preset is configured;
- no local resources are listed, or an indicator or redirect URI is not loopback;
- a client reuses `cratis-cli`.

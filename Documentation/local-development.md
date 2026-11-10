# Local development profile

Run Identity on your machine so a local tool, such as Claude, pi or VS Code, can sign in and call a local Studio or Direct MCP server. The profile is opt-in, runs only in the `Development` environment, and issues tokens that no deployed service accepts.

## What it is

When the profile is on, Identity uses a loopback issuer, its own signing and encryption keys, a set of local resources and a few pre-registered public clients. The tools sign in with the authorization code flow and PKCE, as `cratis-cli` does, and receive an access token for one product. They never receive an ID token.

## What it is not

The profile is not a way to reach a deployed environment. It has no deployed keys, no deployed issuer and no deployed resource indicators, and it never merges with deployed configuration.

## Prerequisites

- A running Chronicle server. The default host settings use `chronicle://localhost:35000`.
- A GitHub OAuth app for the sign-in, described next.

### GitHub OAuth app

Identity signs people in through GitHub. Register an OAuth app once for your machine:

1. In GitHub, open **Settings > Developer settings > OAuth Apps > New OAuth App**.
2. Set **Homepage URL** to `http://localhost:5100/` and **Authorization callback URL** to `http://localhost:5100/signin-github`. The host must listen on the same origin; it does with the launch profile below.
3. Generate a client secret, then store both values as user secrets, which stay outside the repository:

```shell
dotnet user-secrets init --project Source/Identity.Host
dotnet user-secrets set "Identity:GitHub:ClientId" "<client id>" --project Source/Identity.Host
dotnet user-secrets set "Identity:GitHub:ClientSecret" "<client secret>" --project Source/Identity.Host
```

`dotnet user-secrets init` adds a `UserSecretsId` to the project file; leave it out of any commit that is not about secrets. User secrets load in the `Development` environment, the only one the profile runs in. Environment variables `Identity__GitHub__ClientId` and `Identity__GitHub__ClientSecret` work as well. Never put either value in `appsettings.Development.json`.

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
| macOS | `~/Library/Application Support/cratis/identity-dev/<issuer-authority>/` |
| Linux | `$XDG_DATA_HOME/cratis/identity-dev/<issuer-authority>/`, which is `~/.local/share/cratis/identity-dev/<issuer-authority>/` when `XDG_DATA_HOME` is not set |
| Windows | `%LOCALAPPDATA%\cratis\identity-dev\<issuer-authority>\` |

The location is .NET's `LocalApplicationData` folder, which .NET resolves to `~/Library/Application Support` on macOS (it ignores `XDG_DATA_HOME` there).

The key store creates **only the final `<issuer-authority>` directory**. Its entire parent chain must already exist; a missing ancestor is refused with a message asking you to create the parent. The normal host profile ensures the per-user app-data directory and its `cratis/identity-dev` parent during host bootstrap, before handing control to the key store. That bootstrap creates application-data folders, not key material or a store leaf. The store then independently validates every ancestor; bootstrap never exempts a newly created ancestor from the ownership and access checks. A caller provisioning a store directly must prepare its parent first.

### Local key-store threat model

The key store protects against other local users and against accidentally adopting deployed keys. It does not protect against an attacker already running as your user, root or administrator. The marker file and certificates named `NOT-FOR-DEPLOYMENT` make mistakes visible; they are not a security boundary and cannot stop someone who can write the store from forging them.

The complete existing ancestor chain is validated **once, top-down**. Each child is opened without following links, relative to its already validated parent's directory handle. On macOS and Linux every parent must be owned by you or root and not writable by others, except for a **root-owned sticky directory**, such as `/tmp`. A current-user-owned sticky directory does not qualify for that exception. Missing ancestors are never created or skipped by the store. On Windows every parent must be owned by you, SYSTEM, Administrators or TrustedInstaller and must not let anyone else delete or take over what it contains; an unreadable or unsupported access control list is refused.

The validated leaf is retained as a pinned no-follow directory handle for the process lifetime. Its device/inode (or Windows volume/file ID) is checked against the validated identity before key access. Provisioning, runtime signing/encryption key loads and reloads, provenance checks, and the Data Protection XML repository all enumerate, open, verify, create, publish and remove files relative to this custody, never by reopening a pathname. The Data Protection child directory is pinned during provisioning as well. Renaming the store or a parent away, substituting another directory and restoring the original name cannot redirect key operations into the substitute. The host releases these handles on shutdown.

Files are verified on their actual open handles before reading or writing key material, including their owner and access permissions. A hardlink whose permissions were widened is refused. On macOS and Linux new store directories start at `0700`; keys, certificates, Data Protection XML and the marker start at `0600`. On Windows new directories and files receive an owner-only access control list with inheritance disabled; an inheriting or NULL access control list is refused. Native opens refuse links and reparse points. Files are published atomically without replacing an existing destination; an interrupted temporary write is refused rather than adopted.

In this profile ASP.NET Core Data Protection generates its own development ring through the handle-backed XML repository. You do not supply a Data Protection key. Its files receive the same owner-only checks as signing keys; if an older local ring contains files with broader permissions, remove the development store and let the profile recreate it. A deployed host still uses the operator-provisioned secret store, unchanged.

Identity refuses to start, naming the problem, when the directory:

- holds anything that is not a development key file, such as a deployed key ring, or a leftover temporary file (`.tmp-*`) from an interrupted run;
- has a missing parent, is a link, is reached through one, or is the repository content folder or inside it;
- is owned by someone else, is accessible to others, or sits below a directory others can write to or, on Windows, whose access control list cannot be read;
- holds a certificate without its private key.

A private key without its certificate, which an interrupted first run can leave, is removed and recreated. Delete the directory to start over. Never copy, commit or deploy these keys.

New keys are added when the current ones are about to expire. Old ones stay so earlier tokens still validate.

## Local resources and audiences

The settings in `Source/Identity.Host/appsettings.Development.json` replace the deployed resources. They are never merged.

| Product | Audience | Resource indicator | Scopes |
| --- | --- | --- | --- |
| Direct MCP | `direct-mcp` | `http://localhost:5200/mcp` | `direct:mcp:read`, `direct:mcp:content.write`, `direct:mcp:work` |
| Studio MCP | `studio-mcp` | `http://localhost:5300/mcp` | `studio:mcp:read`, `studio:mcp:write` |

The audience for Direct MCP is `direct-mcp`, the same as in the deployed catalog. `direct-api` is the Direct REST audience, and a token for one is refused by the other. The profile does not register `direct-api`. The ports are placeholders. Change the indicators to match how each product runs locally.

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

# Cratis Identity

Cratis Identity is the OAuth 2.1 authorization server for Cratis products. It signs people in (GitHub for now), asks them to approve a client, and issues short-lived, audience-bound JWT access tokens plus rotating refresh tokens. The first client is the `cratis` CLI; the first resource server is Cratis Direct (`/mcp` and `/v1`).

It is built on [OpenIddict](https://documentation.openiddict.com/) 7 as an embedded library. OpenIddict owns the protocol: request validation, PKCE, token formats, rotation, revocation and discovery. Identity owns the policy around it: accounts, which client can reach which resource, tenant selection and consent.

## Where it sits

```text
 cratis CLI ──(browser: code + PKCE)──► Cratis Identity ──(upstream login)──► GitHub
     │                                       │ issues JWT: aud=direct-api, sub, tid, scope, azp
     ▼                                       │ publishes /.well-known/oauth-authorization-server + JWKS
 AuthProxy (per product, edge) ◄─────────────┘
   validates the JWT with its JwtBearer support: issuer, signature, audience per route, expiry;
   derives the tenant from `tid`; forwards trusted identity headers
     ▼
 Direct /mcp and /v1 (resource servers): enforce scope ∩ roles ∩ tenant membership
```

- **Identity is the authority.** Accounts, clients, grants, consent and signing keys live here.
- **AuthProxy stays an edge.** It validates Identity tokens; it does not issue them or hold grant state (Cratis/AuthProxy#143).
- **Products stay resource servers.** A token says who the user is, which tenant they chose, and what the client may attempt. The product decides whether that user may actually do it in that tenant.

## Packages

| Package | Contents |
| --- | --- |
| `Cratis.Identity` | The module: OpenIddict server, stores, `AddCratisIdentity()` and `MapCratisIdentity()`. |
| `Cratis.Identity.Contracts` | The contracts a resource server reads, such as the access token claim types. |
| `@cratis/identity` | TypeScript proxies generated from the module by the Arc proxy generator. |

CI builds and packs all three as artifacts. Nothing is published until the W0 specs (#24, #26) are locked. The supported surface is `AddCratisIdentity`, `MapCratisIdentity` and the `Identity` configuration section; other public types are implementation detail and may become internal.

## Hosting Identity

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddCratisArc();
builder.AddCratisChronicle(options =>
{
    options.EventStore = "Identity";
    options.ConnectionString = builder.Configuration.GetConnectionString("chronicle") ?? "chronicle://localhost:35000";
});
builder.AddCratisIdentity();

var app = builder.Build();
app.MapCratisIdentity();
app.UseCratisArc();
app.UseCratisChronicle();
await app.RunAsync();
```

`MapCratisIdentity` owns the Identity pipeline order: routing → realm scope → security headers → authentication, followed by endpoint mapping. Call it once, before the host's own endpoints and Arc/Chronicle middleware; do not call `UseAuthentication` or `UseRouting` ahead of it for Identity routes. Authentication and generated Identity API routes use the realm namespace; non-Identity routes retain the host's namespace resolver. `Source/Identity.Host` is the sample host and the standalone server that the Docker image runs.

## What v1 implements

| Area | Behavior |
| --- | --- |
| Authorization code flow | Mandatory PKCE, `S256` only (`plain` is refused). No implicit, password or client-credentials grants. |
| Native clients | Pre-registered public client `cratis-cli` (no secret). Redirect URIs `http://127.0.0.1:<ephemeral-port>/callback` and `http://[::1]:<ephemeral-port>/callback` (RFC 8252). The path and IP literal must match; `localhost` is refused. |
| Resource indicators (RFC 8707) | `https://cratis.direct/mcp` maps to audience `direct-mcp`; `https://cratis.direct/v1` maps to `direct-api`. Every grant is bound to exactly one resource server. Without a `resource` parameter the resource is inferred from the scopes, but only when they belong to a single resource server. |
| Scopes | API: `direct:read`, `direct:content.write`, `direct:work`. MCP: `direct:mcp:read`, `direct:mcp:content.write`, `direct:mcp:work`. Add `offline_access` for a refresh token. Requested scopes that belong to another resource server are dropped. There is no approve or admin scope: approval is a domain action in Direct, never a token capability. |
| Refresh tokens | Rotated on every use. Concurrent redemption has a single winner; a failed redemption revokes the grant authorization and its tokens, so even the winner's successor is unusable. Reuse has zero leeway. The family has an absolute lifetime (30 days by default) that rotation does not extend. |
| Revocation (RFC 7009) | `POST /connect/revocation`. Revoking a refresh token revokes the whole grant, including its access tokens for introspection. |
| Introspection (RFC 7662) | `POST /connect/introspect` for configured confidential resource-server clients only. |
| Metadata | `/.well-known/oauth-authorization-server` (RFC 8414), `/.well-known/openid-configuration`, and JWKS at `/.well-known/jwks`. |
| Upstream login | GitHub OAuth app (no GitHub scopes requested). The account is keyed by the numeric GitHub id, which survives login renames; the login is stored and refreshed at each sign-in. |
| Consent | Asked on every authorization. The page shows the client, where the code will be sent, the resource, the scopes and a tenant picker. An optional `tenant` authorization request parameter preselects a tenant only if the signed-in user may choose it; the user can change the choice. It has no scripts, is protected by antiforgery and cannot be framed. `prompt=none` fails with `login_required` or `consent_required`. |
| Tenants | `ITenantDirectory` calls the resource's product tenants callback on every consent listing, consent submission, code redemption and refresh, without caching. Callback failures refuse consent and tokens with `temporarily_unavailable`. Configured tenants are Development fixtures only. |

Protocol obligations, executable proofs, known gaps and the OpenIddict upgrade checklist are in the [conformance catalogue](Documentation/conformance.md).

## Access token contract

Access tokens are signed JWTs (`typ: at+jwt`, RS256), not encrypted. Default lifetime: 15 minutes.

| Claim | Meaning |
| --- | --- |
| `iss` | The configured issuer, exactly. |
| `aud` | The audience of the resource server, for example `direct-api`. Exactly one. |
| `sub` | The canonical Cratis account id (a GUID). Stable across providers and renames. |
| `tid` | The tenant the user chose on the consent page. |
| `scope` | Space-separated granted scopes. |
| `azp`, `client_id` | The client the token was issued to (`cratis-cli`). This is the client's *asserted* identity; a public client cannot prove which program is using it. |
| `github_id`, `github_login` | The linked GitHub user id and the login at issue time. |
| `name`, `preferred_username` | Display name and GitHub login. |
| `exp`, `iat`, `jti` | Standard. |

A resource server must validate `iss`, the signature against the JWKS, `aud`, `exp` and `typ`. It must still check that `sub` is a member of `tid`: Identity records the choice, the product enforces membership.

## Endpoints

| Path | Purpose |
| --- | --- |
| `GET/POST /connect/authorize` | Authorization endpoint: upstream login, consent, code issuance. |
| `POST /connect/token` | Code redemption and refresh. |
| `POST /connect/revocation` | Token revocation. |
| `POST /connect/introspect` | Token introspection for resource servers. |
| `GET /.well-known/oauth-authorization-server` | RFC 8414 authorization server metadata; includes `authorization_response_iss_parameter_supported: true`. |
| `GET /.well-known/openid-configuration` | OpenIddict's discovery fallback. This server does **not** offer the `openid` scope or issue ID tokens in v1. |
| `GET /.well-known/jwks` | Public signing keys. |
| `GET /signin-github` | GitHub OAuth callback. Register it on the GitHub OAuth app. |
| `GET/POST /logout` | Identity site cookie sign-out: GET serves an antiforgery-protected form; POST clears the session cookie. This does not revoke CLI grants. |
| `GET /healthz` | Liveness. |

## Configuration

Everything is under the `Identity` section (environment variables use `Identity__Section__Key`).

| Key | Default | Notes |
| --- | --- | --- |
| `Issuer` | — | Absolute public URI, normally `https://auth.example/` (including trailing `/`, no path). **Required outside development**; the service refuses to start without it. Its scheme and host must match the public authorization/discovery origin. |
| `MongoDB:ConnectionString` | `mongodb://localhost:27017` | |
| `MongoDB:Database` | `cratis-identity` | Holds OpenIddict applications, authorizations and and tokens. Tokens have a TTL index on their expiry; keys are never stored here. |
| `Accounts:ConfirmNoLegacyMongoStore` | `false` | Set to `true` only after confirming there is no legacy MongoDB `accounts` collection to lose. While it is `false`, startup is refused if that collection has documents or cannot be checked (see [Event-sourced accounts](#event-sourced-accounts)). Off by default; set it only after confirming no legacy store exists (and in local dev and test stacks with no MongoDB account store). |
| `GitHub:ClientId`, `GitHub:ClientSecret` | — | GitHub OAuth app. Callback URL: `<issuer>signin-github`. |
| `SecretStore:Path` | — | Directory the platform mounts from its secret store, holding realm signing keys, encryption keys and the Data Protection key ring (see [Keys and the secret store](#keys-and-the-secret-store)). **Required outside development.** The old `SigningCertificate` and `EncryptionCertificate` settings are refused at startup. |
| `SecretStore:ReloadInterval` | `00:01:00` | How often the directory is read again; between one second and five minutes. |
| `Tokens:AccessTokenLifetime` | `00:15:00` | Also the longest a revoked grant keeps working at a resource server that does not introspect. |
| `Tokens:AuthorizationCodeLifetime` | `00:02:00` | |
| `Tokens:RefreshTokenLifetime` | `30.00:00:00` | Absolute lifetime of a refresh-token family. |
| `SessionLifetime` | `01:00:00` | The Identity site session cookie; not sliding. |
| `Resources[]` | Direct | `Audience`, `DisplayName`, `Indicators[]`, `Scopes[]` (`Name`, `Description`), `TenantsCallback` (`Url`, `Secret`, `Timeout`). `Tenants[]` (`Id`, `Name`, `Members[]` as numeric GitHub ids, `AllowAllAccounts`) is Development only and cannot be combined with a callback. Validated at startup: duplicate audiences, indicators or scopes, fragments, and protocol scopes are rejected. Outside Development every resource requires an HTTPS callback and a secret of at least 32 characters. |
| `IntrospectionClients[]` | none | `ClientId`, `ClientSecret` for resource servers that introspect. |

Clients are declared in code (`ClientDescriptors`) and configuration (introspection clients). The client store is reconciled at startup: undeclared clients are deleted.

In development (`ASPNETCORE_ENVIRONMENT=Development`) without a `SecretStore:Path`, Identity uses ephemeral signing and encryption keys and the default Data Protection ring, so tokens stop validating when it restarts, and it allows plain HTTP.

### Event-sourced accounts

Accounts use native Arc commands and Chronicle projections by default; there is no legacy MongoDB account writer or opt-in switch. The host connects through `ConnectionStrings:chronicle` (outside the `Identity` section). Keep MongoDB for OpenIddict clients, grants, tokens and operational metadata. Existing MongoDB account rows are not imported into Chronicle.

**Upgrading from the MongoDB account store.** Identity never imports, changes or deletes the old `accounts` collection, and starting over it would make those accounts disappear. So at startup, before anything else touches MongoDB, Identity does a read-only check of the `accounts` collection in the configured `MongoDB:ConnectionString` and `MongoDB:Database`, and in the historic default (`mongodb://localhost:27017`, `cratis-identity`). It refuses to start if either has documents, and also if either cannot be checked (unreachable, timeout, authentication failure), because it cannot prove nothing would be lost; the error names only "configured" or "default", never a host, credential or database. Export or retire the legacy collection yourself, or, once you have confirmed the data is obsolete or that no legacy store exists, set `Identity:Accounts:ConfirmNoLegacyMongoStore=true` (`Identity__Accounts__ConfirmNoLegacyMongoStore=true`); the check is then skipped with a warning. Development configuration, the compose file and the specs set it. A legacy store under a different server or database name than those two cannot be detected; that is the operator's responsibility. Follow-up export tool: Cratis/Identity#105.

Outside Development, provide `Identity:Accounts:LookupKey` through the platform's secret configuration mechanism: base64 containing at least 256 bits of key material. `LookupKeyRevision` defaults to `1`; changing the revision against an existing realm store is refused. `ReadModelCatchUpTimeout` defaults to 30 seconds and must be positive. Never log or publish the lookup key.

Accounts and permanent upstream-key reservations belong to Chronicle's `Default` realm namespace, not an application's tenant namespace. A verified GitHub callback supplies the immutable numeric upstream id; login, name and email observations cannot establish identity. A new account receives an immutable UUID subject. Unlinking retains the reservation, and disabling or tombstoning an account prevents sign-in.

Every sign-in attempt guards the account event source, waits for the filtered Account and UpstreamKeyLookup projection targets, and rechecks reservation and lifecycle. The native transaction rejects an unlink or disable committed after that recheck. A concurrency conflict gets one fresh decision attempt; a projection timeout fails as `temporarily_unavailable`, without session creation. A session is built only from the caught-up active Account projection.

### Keys and the secret store

Realm signing keys, encryption keys and the Data Protection key ring are read only from `SecretStore:Path`, a directory mounted by the platform (a Kubernetes Secret volume or a CSI secrets-store mount). Keys never live in configuration files, MongoDB or events. **Breaking change before 1.0:** `SigningCertificate` and `EncryptionCertificate` are no longer read; Identity refuses to start if either is still configured.

| File | Content |
| --- | --- |
| `signing.<name>.crt` and `.key` | PEM certificate and unencrypted PKCS#8 PEM private key. RSA of at least 2048 bits, or ECDSA P-256 (ES256). |
| `encryption.<name>.crt` and `.key` | Same, RSA of at least 2048 bits. Encrypts authorization codes and refresh tokens. |
| `data-protection.<name>.xml` | One Data Protection key element (template below). |

Identity validates the whole directory at startup and fails closed on anything invalid: a missing pair, a key that does not match its certificate, an encrypted or malformed key, a weak key, the same key used twice or for both purposes, a file name it does not recognise, no key valid now, or no active Data Protection key. A later reload that finds invalid material keeps the previous keys and logs the file name and reason, never key bytes. Dot entries (Kubernetes `..data`) and sub-directories are ignored.

Generate a pair (the name is yours; never reuse a name for different material):

```sh
openssl req -x509 -newkey rsa:3072 -nodes -keyout signing.2026a.key -out signing.2026a.crt -subj "/CN=Cratis Identity" -days 400
```

`openssl` writes an unencrypted PKCS#8 key. The certificate's validity start is when the key becomes usable, so set it to the intended start (for example with `-not_before` where your OpenSSL supports it).

Data Protection key template. Fill `id` with a new GUID, the three dates (ISO 8601 UTC) and `value` with 64 random bytes, base64:

```xml
<key id="{GUID}" version="1">
  <creationDate>{CREATION}</creationDate>
  <activationDate>{ACTIVATION}</activationDate>
  <expirationDate>{EXPIRATION}</expirationDate>
  <descriptor deserializerType="Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.AuthenticatedEncryptorDescriptorDeserializer, Microsoft.AspNetCore.DataProtection, Version=10.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60">
    <descriptor>
      <encryption algorithm="AES_256_CBC" />
      <validation algorithm="HMACSHA256" />
      <masterKey enc:requiresEncryption="true" xmlns:enc="http://schemas.asp.net/2015/03/dataProtection">
        <value>{64 RANDOM BYTES, BASE64}</value>
      </masterKey>
    </descriptor>
  </descriptor>
</key>
```

The ring is read-only for Identity: it never generates or writes keys, so operators rotate Data Protection keys through the secret store at least every 90 days. Identity logs a warning 14 days before the active key expires, and refuses to start when no key is active.

**Rotation runbook.** A key signs or encrypts once its certificate has been valid for 600 seconds; until then it is only published, so every resource server's JWKS cache can learn it first. Every key stays published (and usable for decryption) until it is removed from the directory. The kid is the certificate thumbprint, so it is stable and never reused.

1. Publish the successor at least 10 minutes before you want it to sign, with its certificate `NotBefore` at the intended start. It takes effect 600 seconds after `NotBefore`, within `ReloadInterval`.
2. Keep a retired signing key for at least the longer of 630 seconds and the access token lifetime after its successor started signing, so tokens it signed still validate.
3. Keep a retired encryption key for at least the refresh token lifetime, so refresh tokens it protected still redeem.
4. Remove the retired files. Identity cannot stop a premature removal; independent retirement tombstones are tracked in [#42](https://github.com/Cratis/Identity/issues/42).

Short-lived state (authorization codes, refresh-token rotations) lives only in MongoDB with a TTL index on its expiry. An entry past its expiry is refused at redemption even before MongoDB deletes it.

### Tenants callback (internal, unstable until contract v1)

This wire contract is **internal; changes without notice until contract v1 (Cratis/Identity#24) locks**. The product owns membership. Identity sends `POST` to the resource's `TenantsCallback:Url`, with `Accept: application/json`, `Authorization: Bearer <Secret>` and JSON:

```json
{"audience":"direct-api","subject":"canonical-account-id","github_id":"134365"}
```

The product authenticates the secret and returns HTTP 200 with `Content-Type: application/json`:

```json
{"subject":"canonical-account-id","tenants":[{"id":"t-1234","name":"My workspace"}]}
```

The subject must match exactly. `github_id` is a legacy membership key pending Direct#2472. Tenant ids must match `^[a-z0-9](?:[a-z0-9-]{0,62}[a-z0-9])?$` without normalization; incompatible ids need a product-side mapping. Responses are limited to 64 KiB and 256 tenants, with unique ids and names of at most 128 characters without control characters. Blank or missing names use the id. Redirects are not followed. Failure or an invalid response means no consent, code or token; `temporarily_unavailable` lets clients retry, including with the same unredeemed refresh token after recovery.

Configure, for example, `Identity__Resources__0__TenantsCallback__Url=https://direct.example/api/internal/identity/tenants`. Supply `Secret` through the secret store or environment, never committed configuration. `Timeout` defaults to three seconds and must be greater than zero and at most ten seconds, covering connection, headers and body. Outside Development HTTPS and a secret of at least 32 characters are required; Development allows HTTP and an empty secret (no Authorization header). Production rollout must wait for an authenticated callback in each product; there is no production configuration-roster fallback.

## Running locally

1. Create a GitHub OAuth app with the callback URL `http://localhost:5100/signin-github`.
2. Create `.env` next to `docker-compose.yml`:

   ```text
   GITHUB_CLIENT_ID=...
   GITHUB_CLIENT_SECRET=...
   GITHUB_USER_ID=...   # gh api user -q .id
   ```

3. Start Identity and MongoDB:

   ```shell
   docker compose up --build
   ```

4. Check the metadata at <http://localhost:5100/.well-known/oauth-authorization-server>.

To run from source instead, start MongoDB on `localhost:27017` and set `Identity__Issuer=http://localhost:5100/`, `Identity__GitHub__ClientId`, `Identity__GitHub__ClientSecret`, `Identity__Resources__0__Tenants__0__Id=local` and `Identity__Resources__0__Tenants__0__Members__0` (your numeric GitHub id). Repeat the tenant settings at `Identity__Resources__1__Tenants__0` for MCP. Then run `ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5100 dotnet run --project Source/Identity.Host`. Your GitHub OAuth app must have the matching callback URL.

The compose file configures one tenant, `local`, for your GitHub user on both Direct resources and binds the HTTP port to loopback only. The Dockerfile builds the app in the .NET 10 SDK and runs it as a non-root user in the .NET 10 chiseled image; compose is **development only**, not a production deployment recipe.

## CLI protocol contract

For the compose setup, the issuer is exactly `http://localhost:5100/` and the discovery URL is `http://localhost:5100/.well-known/oauth-authorization-server`. For a deployed service, the CLI must request `/.well-known/oauth-authorization-server` on the issuer origin (or `/.well-known/openid-configuration` as fallback) and compare the discovered `issuer` with its configured issuer. The returned endpoint URLs are absolute. Authorization responses include `iss` on the loopback redirect, including OAuth error responses; the CLI must compare it to the discovered issuer along with its `state`.

The CLI sends `client_id=cratis-cli`, `response_type=code`, `code_challenge_method=S256`, its challenge, a random `state`, `redirect_uri=http://127.0.0.1:<ephemeral-port>/callback` (or `[::1]`), `scope=direct:mcp:read direct:mcp:content.write direct:mcp:work offline_access` with `resource=https://cratis.direct/mcp`, or `scope=direct:read direct:content.write direct:work offline_access` with `resource=https://cratis.direct/v1` (request only what is needed), and optionally `tenant=<requested-tenant>` to the authorization endpoint. No secret is sent. The user must consent and choose a tenant; the hint cannot grant access. The CLI redeems the code at the discovered token endpoint using the **same** redirect URI, its PKCE verifier and the same `resource` value; refresh requests also send `client_id`, `refresh_token`, `grant_type=refresh_token` and `resource`. It posts `client_id`, `token` and `token_type_hint=refresh_token` to the discovered revocation endpoint on logout. Each resource indicator yields one access-token audience: `direct-mcp` for MCP or `direct-api` for API. A new grant is required to switch tenants; changing a CLI context alone cannot change `tid`.

## Development

```shell
dotnet build Identity.slnx -c Release
./Source/Identity.Specs/bin/Release/net10.0/Cratis.Identity.Specs
./Source/Identity.Integration.Specs/bin/Release/net10.0/Cratis.Identity.Integration.Specs   # needs Docker: starts MongoDB 8.0.14 and Chronicle 19.111.0-development
dotnet pack Identity.slnx -c Release --no-build --output Artifacts/NuGet
(cd Source/JavaScript/Identity && npm ci && npm run build && npm pack --pack-destination ../../../Artifacts/npm)
```

The integration specs host the real pipeline (Arc, Chronicle, OpenIddict, MongoDB, consent, antiforgery, token endpoint). They replace GitHub's network login with a fake upstream that supplies verified observations to the same native command adapter. A test-local progress gate can hold projection acknowledgments without replacing the kernel, command pipeline or filtered target reader. The Chronicle development container is pinned to the client version and exposes only a dynamically assigned loopback port.

## Security notes

- **Public clients are not authenticated.** Anyone can present `client_id=cratis-cli` with a loopback redirect. PKCE protects the code in transit; it does not prove which program asked. The consent page says so, and `azp` is an asserted identity.
- **Consent is the gate.** It is shown every time. Remembered consent and a "connected apps" page come later.
- **Tenancy is chosen here but enforced by the product.** `tid` is what the user selected from the product callback through `ITenantDirectory`; Identity does not cache membership. Resource servers must recheck live membership on every request and must never fall back to a default tenant.
- **Access tokens are self-contained.** Revocation reaches refresh tokens immediately and access tokens only through introspection. Otherwise a revoked access token works until it expires (15 minutes by default).
- **Refresh reuse is strict.** There is no reuse leeway. Even two requests that passed token validation before redemption cannot both receive successors: MongoDB's atomic token update picks at most one winner. A concurrent loser revokes the shared authorization and all existing tokens; a successor created later under that authorization also fails validation. A sequential reuse is caught at token validation, before OpenIddict's own check: the authorization is revoked first and then all its tokens, so a successor that a request which redeemed the token moments earlier stores afterwards also fails validation. The revocation fails closed; if it cannot be completed, the request fails instead of issuing tokens. A client that retries a refresh after a lost response loses the grant and must sign in again. The CLI must serialize refreshes (Cratis/cli#205).
- **Token pruning keeps redeemed refresh tokens for the full refresh lifetime,** so a replayed stolen token is still detected as reuse.
- **Issuer and keys are explicit in production.** The issuer is never derived from the request host, and ephemeral keys are refused. The public origin must agree with the configured issuer and the discovery endpoint URLs; do not configure a path prefix. Behind a TLS-terminating ingress, configure forwarded headers only from known proxies and verify discovery returns public HTTPS URLs (for example `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` only when the ingress is the only route in). Otherwise OpenIddict may refuse non-HTTPS requests or discovery may publish internal URLs.
- **Upstream re-authentication is limited.** `prompt=login` repeats the GitHub OAuth round trip, but GitHub may complete it without asking again. Real step-up needs a stronger authenticator (see the roadmap).
- **GitHub tokens are not kept.** The GitHub access token is used once to read the user and then discarded. OpenIddict logging stays at Warning even in development: its Information messages can include raw OAuth requests with codes or tokens.
- **Browser cookies are protected.** The Identity session cookie is HttpOnly and SameSite=Lax; the antiforgery cookie is HttpOnly and SameSite=Strict. Both are marked Secure outside development. Behind a TLS-terminating ingress this requires forwarded headers (`X-Forwarded-Proto`) to be configured as described above: the service must see the original HTTPS scheme, and antiforgery refuses requests it sees as plain HTTP. Sign out of the site with `/logout`; CLI grants must be revoked separately.
- **Personal data.** Accounts store the GitHub id, login and display name.

## Roadmap

In rough order, following the long-term architecture:

1. **Client ID Metadata Documents (CIMD)** for remote MCP clients. `IClientIdMetadataDocuments` defines the contract: SSRF-safe fetching, `client_id` must equal the document URL, redirect validation, and surfacing through an OpenIddict application store decorator. Advertise `client_id_metadata_document_supported` once it exists.
2. **Accounts, grants and consent as an event-sourced Cratis domain** (Arc and Chronicle) with an audit of grants, consents and revocations, remembered consent, and a "connected apps" page to review and revoke grants.
3. **Product-backed tenant directory — implemented.** Each production resource requires an authenticated product tenants callback; product-side rollout is still required.
4. **Device authorization grant (RFC 8628)** for headless terminals, with polling intervals, `slow_down`, expiry and phishing-resistant confirmation.
5. **Step-up and `max_age`/`acr`**: fresh authentication for sensitive consent, passkeys.
6. **Decoupled approval requests (CIBA-style)**, so agents running unattended can ask a person to approve on another device.
7. **RFC 9396 `authorization_details`** for transaction-bound approvals, for example a specific publication payload hash.
8. **Token exchange (RFC 8693) with `act`** so agents, including Direct's own workers, act for a user with attributable, narrowed tokens.
9. **More upstream providers and customer SSO per tenant** (Microsoft, Google, Apple; OIDC/SAML per customer), with account linking.
10. **Direct browser login through Identity**: AuthProxy becomes an OIDC client of Identity, giving single sign-on across Cratis products. This is when `openid` and identity tokens get turned on.
11. **Operations**: rate limiting on the authorization and token endpoints, and metrics.

## License

MIT. See [LICENSE](LICENSE).

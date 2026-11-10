# Cratis Identity

Cratis Identity is the OAuth 2.1 authorization server for Cratis products. It signs people in (GitHub for now), asks them to approve a client, and issues short-lived, audience-bound JWT access tokens plus rotating refresh tokens. The first client is the `cratis` CLI; the first resource server is Cratis Direct (`/mcp` and `/v1`).

It is built on [OpenIddict](https://documentation.openiddict.com/) 7 as an embedded library. OpenIddict owns the protocol: request validation, PKCE, token formats, rotation, revocation and discovery. Identity owns the policy around it: accounts, which client can reach which resource, tenant selection and consent.

## Where it sits

```text
 cratis CLI ──(browser: code + PKCE)──► Cratis Identity ──(upstream login)──► GitHub
     │                                       │ issues JWT: aud=direct-mcp, sub, tid, scope, azp
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
| `Cratis.Identity` | The module: OpenIddict server, stores, `AddCratisIdentity()`, `UseCratisIdentity()` and `MapCratisIdentity()`. |
| `Cratis.Identity.Contracts` | The contracts a resource server reads, such as the access token claim types. |
| `@cratis/identity` | TypeScript proxies generated from the module by the Arc proxy generator. |

CI builds and packs all three as artifacts. Nothing is published until the W0 specs (#24, #26) are locked. The supported surface is `AddCratisIdentity`, `UseCratisIdentity`, `MapCratisIdentity`, `IdentityEndpointOptions` and the `Identity` configuration section; other public types are implementation detail and may become internal.

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
app.UseCratisIdentity();
app.MapCratisIdentity(options => options.MapSignOut = true);
app.MapGet("/healthz", () => Results.Text("ok"));
app.UseCratisArc();
app.UseCratisChronicle();
await app.RunAsync();
```

`UseCratisIdentity` explicitly installs the Identity pipeline: routing → realm scope → security headers → authentication. Call it once before Arc/Chronicle middleware and endpoint execution; do not put authentication ahead of it for Identity routes. Its security headers apply globally in hosts that opt in. Authentication and generated Identity API routes use the realm namespace; non-Identity routes retain the host's namespace resolver. `MapCratisIdentity` only maps authorization and token endpoints; it adds no middleware, `/`, or `/healthz`. Hosts own their health and root routes, without collisions regardless of mapping order. Sign-out HTML is off by default; opt in with `MapSignOut = true` as above. Call `MapCratisIdentity` once per host: repeated mapping throws rather than changing the sign-out setting shared with realm middleware. Mapping alone does not validate protocol requests. Without the OpenIddict middleware context, authorization and token handlers fail closed with `OpenIddictRequestMissing` before business logic runs; install `UseCratisIdentity` for the required pipeline order. `Source/Identity.Host` remains the sample host and default Docker/compose target. `Source/Identity.CratisId` is the hosted-shared Cratis ID server; its [image contract](Documentation/deployment/cratis-id-image-contract.md) defines configuration, readiness and the D13 prerequisites before production issuance.

## What v1 implements

| Area | Behavior |
| --- | --- |
| Authorization code flow | Mandatory PKCE, `S256` only (`plain` is refused). No implicit, password or client-credentials grants. |
| Native clients | Pre-registered public client `cratis-cli` (no secret). Redirect URIs `http://127.0.0.1:<ephemeral-port>/callback` and `http://[::1]:<ephemeral-port>/callback` (RFC 8252). The path and IP literal must match; `localhost` is refused. |
| Resource indicators (RFC 8707) | `https://cratis.direct/mcp` maps to audience `direct-mcp`, as required by [N1](Documentation/specs/arc-scope-catalog-v1.md#n1-catalog); `https://cratis.direct/v1` maps to the distinct audience `direct-v1`. `direct-mcp` and `direct-v1` are not interchangeable: a token for one is refused by the other's endpoint. Every grant is bound to exactly one resource server. Without a `resource` parameter the resource is inferred from the scopes, but only when they belong to a single resource server. |
| Scopes | API: `direct:read`, `direct:content.write`, `direct:work`. MCP: `direct:mcp:read`, `direct:mcp:content.write`, `direct:mcp:work`. Add `offline_access` for a refresh token. Requested scopes that belong to another resource server are dropped. There is no approve or admin scope: approval is a domain action in Direct, never a token capability. |
| Refresh tokens | Rotated on every use. Concurrent redemption has a single winner; a failed redemption revokes the grant authorization and its tokens, so even the winner's successor is unusable. Reuse has zero leeway. The family has an absolute lifetime (30 days by default) that rotation does not extend. |
| Token exchange (RFC 8693) | Off by default. A configured confidential client exchanges a token from a trusted external issuer for a short-lived access token bound to one resource and one tenant. See [Token exchange](Documentation/token-exchange.md). |
| Local development | Opt-in `Identity:Development` profile for the reference host: loopback issuer, its own persisted keys and pre-registered local clients, refused outside Development. See [Local development](Documentation/local-development.md). |
| Revocation (RFC 7009) | `POST /connect/revocation`. Revoking a refresh token revokes the whole grant, including its access tokens for introspection. |
| Introspection (RFC 7662) | `POST /connect/introspect` for configured confidential resource-server clients only. |
| Metadata | `/.well-known/oauth-authorization-server` (RFC 8414), `/.well-known/openid-configuration`, and JWKS at `/.well-known/jwks`. |
| Upstream login | GitHub OAuth app (no GitHub scopes requested). The account is keyed by the numeric GitHub id, which survives login renames; the login is stored and refreshed at each sign-in. |
| Consent | Asked on every authorization. The page shows the client, where the code will be sent, the resource, the scopes and a tenant picker. An optional `tenant` authorization request parameter preselects a tenant only if the signed-in user may choose it; the user can change the choice. It has no scripts, is protected by antiforgery and cannot be framed. `prompt=none` fails with `login_required` or `consent_required`. |
| Tenants | `ITenantDirectory` calls the resource's product tenants callback on every consent listing, consent submission, code redemption and refresh, without caching. Callback failures refuse consent and tokens with `temporarily_unavailable`. Configured tenants are Development fixtures only. |

Protocol obligations, executable proofs, known gaps and the OpenIddict upgrade checklist are in the [conformance catalogue](Documentation/conformance.md).

## Device authorization

Device authorization is disabled by default. Set `Identity:Device:Enabled=true` before registering Identity to enable the device endpoints, discovery metadata and CLI permissions. The public options property is `IdentityOptions.DeviceGrant`, bound to the `Identity:Device` section. No CLI auto-fallback or device UI in other products is included.

The device receives a high-entropy opaque `device_code`, an eight-character user code from a 20-symbol unambiguous alphabet, `verification_uri` pointing to `/device`, a five-minute `expires_in`, and a five-second polling `interval`. `verification_uri_complete` is deliberately omitted: users enter the code manually. Polling before the current interval returns `slow_down` and increases it by five seconds; unapproved requests return `authorization_pending`. Approval is explicit, requires a live signed-in account and tenant membership, and remains bound to the originating client, audience, scopes and browser session. Device redemption rechecks grant fences, the independent recovery epoch, token and authorization stamps, account state, and live tenancy. A code cannot be redeemed twice.

The script-free verification page emphasizes the issuer host, client name and asserted client id, scope descriptions and signed-in GitHub login. GET never approves. Session-bound confirmation and antiforgery are required for every POST. Codes are never included in redirect URLs or application logs; verification responses use `no-store` and `no-referrer`.

Default shared MongoDB limits are five attempts per code and per signed-in account, thirty per client IP and a realm-wide ceiling of one thousand guesses per five-minute window. Issuance allows ten requests per client IP per minute. The account budget is primary; the IP budget is secondary. Throttle refusal returns HTTP 429. Storage failure or an unavailable trusted client IP fails closed. Configure trusted forwarded headers in the host, **before** `MapCratisIdentity`, with explicit known proxies/networks; Identity does not trust arbitrary forwarded IP headers. All replicas must share the operational database.

`Identity:Device` also accepts `CodeLifetime`, `PollingInterval`, `VerificationWindow`, `AttemptsPerCode`, `AttemptsPerAccount`, `AttemptsPerIp`, `AttemptsGlobally`, `IssuancesPerIp`, `UserCodeLength` and `UserCodeCharset`. Codes require at least eight symbols and an alphabet of at least twenty distinct uppercase ASCII letters or digits. Keep the default alphabet to avoid ambiguous characters. Limits and durations are validated at startup.

Rate-counter keys are purpose-separated SHA-256 digests, not HMACs; counters use atomic increment/upsert with logical expiry independent of TTL cleanup. Polling alone retains compare-and-swap because it must update the deadline and cumulative interval together, not just increment a counter. OpenIddict token rows remain authoritative for device/user codes, approval and expiry; the operational collection contains only rate and polling state. Its zero-delay TTL index is housekeeping, never the admission decision. Durable consent events and a connected-apps UI are separate work.

## Sender-bound token issuance (opt-in)

RFC 9449 token-endpoint DPoP is disabled by default. Set `Identity:DPoP:Enabled=true` and `Identity:DPoP:PublicTokenEndpoint` to the **exact public token endpoint URL** before registering Identity. The endpoint is never inferred from `Host`, forwarded headers or an internal URL. HTTPS is required; Development permits loopback HTTP. Only ES256 proofs with public P-256 keys are supported. Enabled discovery advertises `dpop_signing_alg_values_supported: ["ES256"]`.

`Identity:DPoP:RequireForAllClients=true` requires proofs for new supported grants. Alternatively, `Identity:DPoP:RequiredClients` lists registered clients (currently `cratis-cli`). Optional clients without a proof retain Bearer behavior; any supplied proof must validate. Organization policy, token exchange and `client_credentials` are not included.

Send a compact proof in one `DPoP` header, with `typ=dpop+jwt`, `alg=ES256`, a public `jwk`, `htm=POST`, the exact configured `htu`, an integer `iat` and a fresh bounded `jti`. The fixed proof lifetime is 300 seconds, future skew 30 seconds and shared replay reservation 390 seconds from acceptance. Do not reuse a proof on retries. A missing or stale server nonce returns `use_dpop_nonce` and `DPoP-Nonce`, with no grant consumption. Retry with that nonce and a **new proof/jti**. Nonces are stateless, origin- and key-bound, using the issuer's existing shared ASP.NET Core Data Protection ring with purpose `Cratis.Identity.Dpop.Nonce.v1`. Retain old ring keys across rotation; all replicas must share the same ring/application discriminator. No new secret-store entry is needed. The current 300-second issuance window overlaps its predecessor by 30 seconds; timestamps at age 330 seconds are refused.

Authorization-code redemption and approved device-code polling issue access tokens with object-valued `cnf: { "jkt": "..." }` and `token_type=DPoP`. Refresh entries carry immutable operational family binding; every rotation requires the same key. Missing/wrong proofs, nonce challenges and replay-store outages refuse issuance **before** consuming a refresh entry. An existing unbound family presented with a proof is refused; requiring DPoP later requires reauthentication, not key attachment during refresh. Disabling DPoP does not downgrade existing bound families to Bearer. ID tokens, authorization/device codes and refresh-token payloads do not carry `cnf`.

**This slice is issuer-only. AuthProxy remains Bearer-only and is not a DPoP resource server.** Do not enable DPoP for clients targeting AuthProxy or assume sender binding is enforced at an API. Resource-server validation of `DPoP` authorization, proof `ath`, nonce and replay handling is deferred. See [the profile](Documentation/specs/identity-dpop-v1.md) and [conformance coverage](Documentation/conformance.md).

## Access token contract

Access tokens are signed JWTs (`typ: at+jwt`, RS256), not encrypted. Default lifetime: 15 minutes.

| Claim | Meaning |
| --- | --- |
| `iss` | The configured issuer, exactly. |
| `aud` | The audience of the resource server, for example `direct-mcp`. Exactly one. |
| `sub` | The canonical Cratis account id (a GUID). Stable across providers and renames. |
| `tid` | The tenant the user chose on the consent page. |
| `scope` | Space-separated granted scopes. |
| `azp`, `client_id` | The client the token was issued to (`cratis-cli`). This is the client's *asserted* identity; a public client cannot prove which program is using it. |
| `github_id`, `github_login` | The linked GitHub user id and the login at issue time. |
| `name`, `preferred_username` | Display name and GitHub login. |
| `exp`, `iat`, `jti` | Standard. |
| `cnf` | Only for opt-in DPoP issuance: JSON object with the proof key's RFC 7638 `jkt`. Never a JSON string. |

A resource server must validate `iss`, the signature against the JWKS, `aud`, `exp` and `typ`. It must still check that `sub` is a member of `tid`: Identity records the choice, the product enforces membership.

## Endpoints

| Path | Purpose |
| --- | --- |
| `GET/POST /connect/authorize` | Authorization endpoint: upstream login, consent, code issuance. |
| `POST /connect/token` | Authorization-code and device-code redemption, and refresh. |
| `POST /connect/device` | Opt-in RFC 8628 device authorization for the public CLI client. |
| `GET/POST /device` | Opt-in signed-in manual code entry, explicit approval or denial, and tenant selection. GET never approves access. |
| `POST /connect/revocation` | Token revocation. |
| `POST /connect/introspect` | Token introspection for resource servers. |
| `GET /.well-known/oauth-authorization-server` | RFC 8414 authorization server metadata; includes `authorization_response_iss_parameter_supported: true`. |
| `GET /.well-known/openid-configuration` | OpenIddict's discovery fallback. This server does **not** offer the `openid` scope or issue ID tokens in v1. |
| `GET /.well-known/jwks` | Public signing keys. |
| `GET /signin-github` | GitHub OAuth callback. Register it on the GitHub OAuth app. |
| `GET/POST /logout` (opt-in) | Identity site cookie sign-out: GET serves an antiforgery-protected form; POST clears the session cookie. This does not revoke CLI grants. |
| `GET /healthz` (host-owned) | Liveness in the sample and Cratis ID hosts, not mapped by the package. |

Set `Identity:Endpoints:RoutePrefix` before `AddCratisIdentity` to move the four `/connect/*` endpoints and optional `/logout`, for example to `/id/connect/token`. The default is empty. Prefixes must be at most 128 ASCII unreserved path characters, start with one `/`, and have no trailing slash, empty or dot segments, escapes, whitespace, query, fragment or backslash. `/.well-known`, `/signin-github`, `/healthz` and `/readyz` prefixes are reserved (including descendants). Invalid shapes fail registration and mapping. A mapping delegate that supplies a different prefix fails rather than diverging from OpenIddict. Discovery/JWKS, the issuer and the registered GitHub callback remain unprefixed. Arc-generated API routes also stay at their original paths; realm matching does not prepend the protocol prefix to them. GitHub sign-in failure or denial may resume only at `{PathBase}{RoutePrefix}/connect/authorize?…`, using the registered endpoint options.

The validated prefix is available through `IOptions<IdentityEndpointOptions>` for token-endpoint middleware, including rate limiting. This base has no token limiter; this is its integration boundary, not a claim that prefixed limiting has been verified. A limiter added later must match `{RoutePrefix}/connect/token` using those options.

## Configuration

Everything is under the `Identity` section (environment variables use `Identity__Section__Key`).

| Key | Default | Notes |
| --- | --- | --- |
| `Endpoints:RoutePrefix` | empty | Validated endpoint prefix, shared by mapping, OpenIddict and realm matching through `IOptions<IdentityEndpointOptions>`. |
| `Issuer` | — | Absolute public URI, normally `https://auth.example/` (including trailing `/`, no path). **Required outside development**; the service refuses to start without it. Its scheme and host must match the public authorization/discovery origin. |
| `MongoDB:ConnectionString` | `mongodb://localhost:27017` | |
| `MongoDB:Database` | `cratis-identity` | Holds OpenIddict applications, authorizations and and tokens. Tokens have a TTL index on their expiry; keys are never stored here. |
| `Accounts:ConfirmNoLegacyMongoStore` | `false` | Set to `true` only after confirming there is no legacy MongoDB `accounts` collection to lose. While it is `false`, startup is refused if that collection has documents or cannot be checked (see [Event-sourced accounts](#event-sourced-accounts)). Off by default; set it only after confirming no legacy store exists (and in local dev and test stacks with no MongoDB account store). |
| `GitHub:ClientId`, `GitHub:ClientSecret` | — | GitHub OAuth app. Callback URL: `<issuer>signin-github`. |
| `SecretStore:Path` | — | Directory the platform mounts from its secret store, holding realm signing keys, encryption keys and the Data Protection key ring (see [Keys and the secret store](#keys-and-the-secret-store)). **Required outside development.** The old `SigningCertificate` and `EncryptionCertificate` settings are refused at startup. |
| `SecretStore:ReloadInterval` | `00:01:00` | How often the directory is read again; between one second and five minutes. |
| `Tokens:AccessTokenLifetime` | `00:05:00` | Startup refuses zero or more than ten minutes. Also the longest a revoked grant keeps working at a resource server that does not introspect. |
| `Tokens:AuthorizationCodeLifetime` | `00:02:00` | |
| `Tokens:RefreshTokenLifetime` | `30.00:00:00` | Absolute lifetime of a refresh-token family. |
| `TokenExchange` | none | `Issuers[]` and `Clients[]` for token exchange; nothing is enabled until both are configured. See [Token exchange](Documentation/token-exchange.md). |
| `SessionLifetime` | `01:00:00` | The Identity site session cookie; not sliding. |
| `DeviceGrant:Enabled` | `true` | Enables the RFC 8628 device authorization grant for `cratis-cli`. |
| `DeviceGrant:CodeLifetime` | `00:10:00` | User and device codes expire together, between one and fifteen minutes; approval does not extend this deadline. |
| `DeviceGrant:PollingInterval` | `00:00:05` | Whole seconds, between five and sixty. Each early poll adds five seconds to subsequent intervals (`slow_down`). |
| `DeviceGrant:VerificationWindow` | `00:10:00` | One-to-sixty-minute verification attempt window. |
| `DeviceGrant:AttemptsPerCode`, `AttemptsPerAccount`, `AttemptsPerIp` | `5`, `20`, `30` | All three budgets apply before code lookup. Upper bounds are 10, 100 and 300 respectively. |
| `Resources[]` | Direct | `Audience`, `DisplayName`, `Indicators[]`, `Scopes[]` (`Name`, `Description`), `TenantsCallback` (`Url`, `Secret`, `Timeout`). `Tenants[]` (`Id`, `Name`, `Members[]` as numeric GitHub ids, `AllowAllAccounts`) is Development only and cannot be combined with a callback. Validated at startup: duplicate audiences, indicators or scopes, fragments, and protocol scopes are rejected. Outside Development every resource requires an HTTPS callback and a secret of at least 32 characters. |
| `IntrospectionClients[]` | none | `ClientId`, `ClientSecret` for confidential resource servers that introspect. Use the token's audience as the client id: `direct-mcp` for `direct:mcp:*` tokens, `direct-v1` for `direct:*` API tokens. Configure a separate secret for each client through the platform's secret configuration mechanism. |

OpenIddict rejects introspection by a client that is neither the token's audience nor its presenter. Application-specific claims are returned only to a confidential audience client. The integration fixture registers `direct-mcp` with a test-only secret because its default CLI flow requests `direct:mcp:read` and issues tokens for `direct-mcp` with presenter `cratis-cli`.

Clients are declared in code (`ClientDescriptors`) and configuration (introspection clients). The client store is reconciled at startup: undeclared clients are deleted.

In development (`ASPNETCORE_ENVIRONMENT=Development`) without a `SecretStore:Path`, Identity uses ephemeral signing and encryption keys and the default Data Protection ring, so tokens stop validating when it restarts, and it allows plain HTTP.

### Device authorization

Request the resource and its scopes at `POST /connect/device`, then display the returned `user_code` and `verification_uri` separately. Identity deliberately omits `verification_uri_complete`: codes belong in the verification form, not URLs, browser history or referrers. Poll `/connect/token` with `grant_type=urn:ietf:params:oauth:grant-type:device_code`, `client_id` and `device_code`, respecting the advertised interval and `slow_down`. An unapproved request returns `authorization_pending`; denial returns `access_denied`, and expiry returns `expired_token`.

The user signs in to Identity, enters the code, reviews the client, resource and scopes, and selects a permitted tenant. Approval is explicit, protected by antiforgery, and bound to the displayed request and approving browser session. Codes are single-use. Device authorization requires full recovery readiness and current immutable recovery-epoch stamps; redemption also rechecks the grant fence, active account and tenant membership. Decisions currently use operational OpenIddict entries; consent and grant lifecycle event publication is separate work.

The host owner must configure ASP.NET Core `UseForwardedHeaders` **before** `MapCratisIdentity` when serving behind a proxy, explicitly setting known proxies/networks. Do not trust arbitrary forwarded IP headers. The account budget is the primary verification throttle; the host-resolved client IP budget is secondary. A missing peer IP, unavailable primary store or unresolved CAS contention refuses verification. Rate windows live in MongoDB with logical expiry and TTL cleanup; their identifiers use purpose-separated HMACs under the existing host-owned account lookup key, which must be stable across replicas. Rotating that key starts new rate windows. Exclude device authorization request/response bodies from host and proxy logging, and share the Data Protection ring across replicas.

### Event-sourced accounts

Accounts use native Arc commands and Chronicle projections by default; there is no legacy MongoDB account writer or opt-in switch. The reference `Identity.Host` connects through `ConnectionStrings:chronicle` (default `chronicle://localhost:35000`, outside the `Identity` section). The hosted Cratis ID image instead binds the `Cratis:Chronicle` section (`ConnectionString`, `EventStore`; environment form `Cratis__Chronicle__ConnectionString`) and ignores `ConnectionStrings:chronicle`. Keep MongoDB for OpenIddict clients, grants, tokens and operational metadata. Existing MongoDB account rows are not imported into Chronicle.

**Upgrading from the MongoDB account store.** Identity never imports, changes or deletes the old `accounts` collection, and starting over it would make those accounts disappear. So at startup, before anything else touches MongoDB, Identity does a read-only check of the `accounts` collection in the configured `MongoDB:ConnectionString` and `MongoDB:Database`, and in the historic default (`mongodb://localhost:27017`, `cratis-identity`). It refuses to start if either has documents, and also if either cannot be checked (unreachable, timeout, authentication failure), because it cannot prove nothing would be lost; the error names only "configured" or "default", never a host, credential or database. Export or retire the legacy collection yourself, or, once you have confirmed the data is obsolete or that no legacy store exists, set `Identity:Accounts:ConfirmNoLegacyMongoStore=true` (`Identity__Accounts__ConfirmNoLegacyMongoStore=true`); the check is then skipped with a warning. Development configuration, the compose file and the specs set it. A legacy store under a different server or database name than those two cannot be detected; that is the operator's responsibility. Follow-up export tool: Cratis/Identity#105.

Outside Development, provide `Identity:Accounts:LookupKey` through the platform's secret configuration mechanism: standard base64 (not base64url, not a plain password) that decodes to at least 32 bytes (256 bits); generate it by base64-encoding 32 random bytes. `LookupKeyRevision` defaults to `1`; changing the revision against an existing realm store is refused. `ReadModelCatchUpTimeout` defaults to 30 seconds and must be positive. Never log or publish the lookup key.

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

The CLI sends `client_id=cratis-cli`, `response_type=code`, `code_challenge_method=S256`, its challenge, a random `state`, `redirect_uri=http://127.0.0.1:<ephemeral-port>/callback` (or `[::1]`), `scope=direct:mcp:read direct:mcp:content.write direct:mcp:work offline_access` with `resource=https://cratis.direct/mcp`, or `scope=direct:read direct:content.write direct:work offline_access` with `resource=https://cratis.direct/v1` (request only what is needed), and optionally `tenant=<requested-tenant>` to the authorization endpoint. No secret is sent. The user must consent and choose a tenant; the hint cannot grant access. The CLI redeems the code at the discovered token endpoint using the **same** redirect URI, its PKCE verifier and the same `resource` value; refresh requests also send `client_id`, `refresh_token`, `grant_type=refresh_token` and `resource`. It posts `client_id`, `token` and `token_type_hint=refresh_token` to the discovered revocation endpoint on logout. Each resource indicator yields one access-token audience: `direct-api` for MCP or `direct-v1` for API. A new grant is required to switch tenants; changing a CLI context alone cannot change `tid`.

## Development

```shell
dotnet build Identity.slnx -c Release
./Source/Identity.Specs/bin/Release/net10.0/Cratis.Identity.Specs
./Source/Identity.Integration.Specs/bin/Release/net10.0/Cratis.Identity.Integration.Specs   # needs Docker: starts MongoDB 8.0.14 and Chronicle 19.144.0-development
dotnet pack Identity.slnx -c Release --no-build --output Artifacts/NuGet
(cd Source/JavaScript/Identity && npm ci && npm run build && npm pack --pack-destination ../../../Artifacts/npm)
```

The integration specs host the real pipeline (Arc, Chronicle, OpenIddict, MongoDB, consent, antiforgery, token endpoint). They replace GitHub's network login with a fake upstream that supplies verified observations to the same native command adapter. A test-local progress gate can hold projection acknowledgments without replacing the kernel, command pipeline or filtered target reader. The Chronicle development container is pinned to the client version and exposes only a dynamically assigned loopback port.

Every change to the OpenIddict server or validation configuration (grant types, flows, event handlers, options, endpoint wiring) ships a host-startup unit spec in the same change: no containers, a service collection with `AddCratisIdentity` and the feature enabled, resolving the OpenIddict server options. Review reads code and does not run it, and the integration specs run only in CI, so only this spec catches a host that cannot start before the push.

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
4. **Device authorization grant (RFC 8628) — opt-in.** Headless terminals use manual-code confirmation, bounded polling and explicit tenant selection. CLI fallback and durable consent history remain separate work.
5. **Step-up and `max_age`/`acr`**: fresh authentication for sensitive consent, passkeys.
6. **Decoupled approval requests (CIBA-style)**, so agents running unattended can ask a person to approve on another device.
7. **RFC 9396 `authorization_details`** for transaction-bound approvals, for example a specific publication payload hash.
8. **Token exchange (RFC 8693) with `act`** so agents, including Direct's own workers, act for a user with attributable, narrowed tokens.
9. **More upstream providers and customer SSO per tenant** (Microsoft, Google, Apple; OIDC/SAML per customer), with account linking.
10. **Direct browser login through Identity**: AuthProxy becomes an OIDC client of Identity, giving single sign-on across Cratis products. This is when `openid` and identity tokens get turned on.
11. **Operations**: rate limiting on the authorization and token endpoints, and metrics.

## License

MIT. See [LICENSE](LICENSE).

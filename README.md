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

## What v1 implements

| Area | Behavior |
| --- | --- |
| Authorization code flow | Mandatory PKCE, `S256` only (`plain` is refused). No implicit, password or client-credentials grants. |
| Native clients | Pre-registered public client `cratis-cli` (no secret). Redirect URIs `http://127.0.0.1:<ephemeral-port>/callback` and `http://[::1]:<ephemeral-port>/callback` (RFC 8252). The path and IP literal must match; `localhost` is refused. |
| Resource indicators (RFC 8707) | `https://cratis.direct/mcp` and `https://cratis.direct/v1` both map to audience `direct-api`. Every grant is bound to exactly one resource server. Without a `resource` parameter the resource is inferred from the scopes, but only when they belong to a single resource server. |
| Scopes | `direct:read`, `direct:content.write`, `direct:work`, plus `offline_access` for a refresh token. Requested scopes that belong to another resource server are dropped. There is no approve or admin scope: approval is a domain action in Direct, never a token capability. |
| Refresh tokens | Rotated on every use. Concurrent redemption has a single winner; a failed redemption revokes the grant authorization and its tokens, so even the winner's successor is unusable. Reuse has zero leeway. The family has an absolute lifetime (30 days by default) that rotation does not extend. |
| Revocation (RFC 7009) | `POST /connect/revocation`. Revoking a refresh token revokes the whole grant, including its access tokens for introspection. |
| Introspection (RFC 7662) | `POST /connect/introspect` for configured confidential resource-server clients only. |
| Metadata | `/.well-known/oauth-authorization-server` (RFC 8414), `/.well-known/openid-configuration`, and JWKS at `/.well-known/jwks`. |
| Upstream login | GitHub OAuth app (no GitHub scopes requested). The account is keyed by the numeric GitHub id, which survives login renames; the login is stored and refreshed at each sign-in. |
| Consent | Asked on every authorization. The page shows the client, where the code will be sent, the resource, the scopes and a tenant picker. An optional `tenant` authorization request parameter preselects a tenant only if the signed-in user may choose it; the user can change the choice. It has no scripts, is protected by antiforgery and cannot be framed. `prompt=none` fails with `login_required` or `consent_required`. |
| Tenants | `ITenantDirectory` lists the tenants an account may pick for a resource. v1 reads them from configuration and fails closed: an account must be a listed member unless the tenant sets `AllowAllAccounts`. Every code redemption and refresh re-checks the account and the tenant. |

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
| `MongoDB:Database` | `cratis-identity` | Holds OpenIddict applications, authorizations and tokens, `accounts`, and `data-protection-keys`. |
| `GitHub:ClientId`, `GitHub:ClientSecret` | — | GitHub OAuth app. Callback URL: `<issuer>signin-github`. |
| `SigningCertificate:Path`, `:Password` | — | PKCS#12 file that signs tokens. **Required outside development.** |
| `EncryptionCertificate:Path`, `:Password` | — | PKCS#12 file that encrypts codes and refresh tokens and protects the Data Protection key ring. **Required outside development.** |
| `Tokens:AccessTokenLifetime` | `00:15:00` | Also the longest a revoked grant keeps working at a resource server that does not introspect. |
| `Tokens:AuthorizationCodeLifetime` | `00:02:00` | |
| `Tokens:RefreshTokenLifetime` | `30.00:00:00` | Absolute lifetime of a refresh-token family. |
| `SessionLifetime` | `01:00:00` | The Identity site session cookie; not sliding. |
| `Resources[]` | Direct | `Audience`, `DisplayName`, `Indicators[]`, `Scopes[]` (`Name`, `Description`), `Tenants[]` (`Id`, `Name`, `Members[]` as numeric GitHub ids, `AllowAllAccounts`). Validated at startup: duplicate audiences, indicators or scopes, fragments, and protocol scopes are rejected. |
| `IntrospectionClients[]` | none | `ClientId`, `ClientSecret` for resource servers that introspect. |

Clients are declared in code (`ClientDescriptors`) and configuration (introspection clients). The client store is reconciled at startup: undeclared clients are deleted.

In development (`ASPNETCORE_ENVIRONMENT=Development`) Identity uses ephemeral signing and encryption keys, so tokens stop validating when it restarts, and it allows plain HTTP.

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

To run from source instead, start MongoDB on `localhost:27017` and set `Identity__Issuer=http://localhost:5100/`, `Identity__GitHub__ClientId`, `Identity__GitHub__ClientSecret`, `Identity__Resources__0__Tenants__0__Id=local` and `Identity__Resources__0__Tenants__0__Members__0` (your numeric GitHub id). Then run `ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5100 dotnet run --project Source/Identity`. Your GitHub OAuth app must have the matching callback URL.

The compose file configures one tenant, `local`, for your GitHub user and binds the HTTP port to loopback only. The Dockerfile builds the app in the .NET 10 SDK and runs it as a non-root user in the .NET 10 chiseled image; compose is **development only**, not a production deployment recipe.

## CLI protocol contract

For the compose setup, the issuer is exactly `http://localhost:5100/` and the discovery URL is `http://localhost:5100/.well-known/oauth-authorization-server`. For a deployed service, the CLI must request `/.well-known/oauth-authorization-server` on the issuer origin (or `/.well-known/openid-configuration` as fallback) and compare the discovered `issuer` with its configured issuer. The returned endpoint URLs are absolute. Authorization responses include `iss` on the loopback redirect, including OAuth error responses; the CLI must compare it to the discovered issuer along with its `state`.

The CLI sends `client_id=cratis-cli`, `response_type=code`, `code_challenge_method=S256`, its challenge, a random `state`, `redirect_uri=http://127.0.0.1:<ephemeral-port>/callback` (or `[::1]`), `scope=direct:read direct:content.write direct:work offline_access` (request only what is needed), `resource=https://cratis.direct/mcp` or `https://cratis.direct/v1`, and optionally `tenant=<requested-tenant>` to the authorization endpoint. No secret is sent. The user must consent and choose a tenant; the hint cannot grant access. The CLI redeems the code at the discovered token endpoint using the **same** redirect URI, its PKCE verifier and the same `resource` value; refresh requests also send `client_id`, `refresh_token`, `grant_type=refresh_token` and `resource`. It posts `client_id`, `token` and `token_type_hint=refresh_token` to the discovered revocation endpoint on logout. Both resource indicators yield one access-token audience: `direct-api`. A new grant is required to switch tenants; changing a CLI context alone cannot change `tid`.

## Development

```shell
dotnet build Identity.slnx -c Release
dotnet test Source/Identity.Specs
dotnet test Source/Identity.Integration.Specs   # needs Docker: starts MongoDB 8.0.14 with Testcontainers
```

The integration specs host the real pipeline (OpenIddict, MongoDB, consent, antiforgery, token endpoint) and replace only GitHub with a fake upstream login that uses the same account linking.

## Security notes

- **Public clients are not authenticated.** Anyone can present `client_id=cratis-cli` with a loopback redirect. PKCE protects the code in transit; it does not prove which program asked. The consent page says so, and `azp` is an asserted identity.
- **Consent is the gate.** It is shown every time. Remembered consent and a "connected apps" page come later.
- **Tenancy is chosen here but enforced by the product.** `tid` is what the user selected from `ITenantDirectory`. Resource servers must check membership and must never fall back to a default tenant.
- **Access tokens are self-contained.** Revocation reaches refresh tokens immediately and access tokens only through introspection. Otherwise a revoked access token works until it expires (15 minutes by default).
- **Refresh reuse is strict.** There is no reuse leeway. Even two requests that passed token validation before redemption cannot both receive successors: MongoDB's atomic token update picks at most one winner. A concurrent loser revokes the shared authorization and all existing tokens; a successor created later under that authorization also fails validation. A sequential reuse is caught at token validation, before OpenIddict's own check: the authorization is revoked first and then all its tokens, so a successor that a request which redeemed the token moments earlier stores afterwards also fails validation. The revocation fails closed; if it cannot be completed, the request fails instead of issuing tokens. A client that retries a refresh after a lost response loses the grant and must sign in again. The CLI must serialize refreshes (Cratis/cli#205).
- **Token pruning keeps redeemed refresh tokens for the full refresh lifetime,** so a replayed stolen token is still detected as reuse.
- **Issuer and keys are explicit in production.** The issuer is never derived from the request host, and ephemeral keys are refused. The public origin must agree with the configured issuer and the discovery endpoint URLs; do not configure a path prefix. Behind a TLS-terminating ingress, configure forwarded headers only from known proxies and verify discovery returns public HTTPS URLs (for example `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` only when the ingress is the only route in). Otherwise OpenIddict may refuse non-HTTPS requests or discovery may publish internal URLs.
- **Upstream re-authentication is limited.** `prompt=login` repeats the GitHub OAuth round trip, but GitHub may complete it without asking again. Real step-up needs a stronger authenticator (see the roadmap).
- **GitHub tokens are not kept.** The GitHub access token is used once to read the user and then discarded. OpenIddict logging stays at Warning even in development: its Information messages can include raw OAuth requests with codes or tokens.
- **Browser cookies are protected.** The Identity session cookie is HttpOnly and SameSite=Lax; the antiforgery cookie is HttpOnly and SameSite=Strict. Both are Secure outside development, even behind an HTTP-to-HTTPS ingress. Sign out of the site with `/logout`; CLI grants must be revoked separately.
- **Personal data.** Accounts store the GitHub id, login and display name.

## Roadmap

In rough order, following the long-term architecture:

1. **Client ID Metadata Documents (CIMD)** for remote MCP clients. `IClientIdMetadataDocuments` defines the contract: SSRF-safe fetching, `client_id` must equal the document URL, redirect validation, and surfacing through an OpenIddict application store decorator. Advertise `client_id_metadata_document_supported` once it exists.
2. **Accounts, grants and consent as an event-sourced Cratis domain** (Arc and Chronicle) with an audit of grants, consents and revocations, remembered consent, and a "connected apps" page to review and revoke grants.
3. **Product-backed tenant directory.** An `ITenantDirectory` that calls the resource server (for example Direct's selectable-tenants endpoint) instead of configuration.
4. **Device authorization grant (RFC 8628)** for headless terminals, with polling intervals, `slow_down`, expiry and phishing-resistant confirmation.
5. **Step-up and `max_age`/`acr`**: fresh authentication for sensitive consent, passkeys.
6. **Decoupled approval requests (CIBA-style)**, so agents running unattended can ask a person to approve on another device.
7. **RFC 9396 `authorization_details`** for transaction-bound approvals, for example a specific publication payload hash.
8. **Token exchange (RFC 8693) with `act`** so agents, including Direct's own workers, act for a user with attributable, narrowed tokens.
9. **More upstream providers and customer SSO per tenant** (Microsoft, Google, Apple; OIDC/SAML per customer), with account linking.
10. **Direct browser login through Identity**: AuthProxy becomes an OIDC client of Identity, giving single sign-on across Cratis products. This is when `openid` and identity tokens get turned on.
11. **Operations**: signing-key rotation without downtime, rate limiting on the authorization and token endpoints, and metrics.

## License

MIT. See [LICENSE](LICENSE).

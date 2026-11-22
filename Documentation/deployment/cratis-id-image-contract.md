---
title: Cratis ID image contract
description: Configure the hosted-shared Cratis ID image, its storage and ingress boundaries, and its staging and production prerequisites.
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Cratis ID is the hosted-shared Identity server for the products realm. `Source/Identity.CratisId` builds `Cratis.Id.Host.dll`; `Identity.Host` remains the development/reference host. This contract gives infrastructure owners the image, configuration and readiness boundary, not deployment manifests or permission to issue production credentials.

The **same image serves staging and production**. Run both with `ASPNETCORE_ENVIRONMENT=Production`, distinct issuer origins, realm keys, accounts and stores. Do not use Development to bypass startup requirements in staging. The single `Identity:Hosting:Preset` is `hosted-shared`; per-application and self-hosted presets are deferred.

## Image and process

The working image name is `ghcr.io/cratis/id`, isolated to the image workflow so it can be renamed. The image workflow builds and smokes pull requests without signing in to a registry or pushing. Releases come from the `Publish` workflow: a merged pull request carrying one of `major`, `minor` or `patch` cuts a release, and `no-release` publishes nothing. Every release enters the publishing job. Only that job requests `packages: write` and uses `GITHUB_TOKEN`. It publishes the release version, the commit SHA and, for non-prerelease versions, `latest`. Deployment should select an immutable digest; publishing an image does not deploy it.

Build the hosted image from the repository root:

```bash
docker build -f Docker/Dockerfile --target cratis-id --build-arg HOST=Identity.CratisId -t cratis-id:local .
```

Both arguments matter: `HOST` selects the project to publish; `--target cratis-id` selects its entrypoint. Without them, the final `reference` target and `Identity.Host` project preserve local compose behavior.

The runtime is .NET 11 ASP.NET Core, chiseled and non-root, listening on HTTP port `8080`. It has no shell or package manager and contains no realm keys, upstream credentials or provisioned databases. Mount secrets read-only with permissions readable by the image's non-root user. No Docker socket, privileged mode or container-managed MongoDB/Chronicle is part of the server contract.

## Required configuration

ASP.NET Core configuration providers supply the following settings. Environment variables replace `:` with `__`; array entries use numeric indexes. Empty shipped values are intentional, not usable defaults. Do not put secret values into the image, source control, command-line arguments or logs.

| Setting | Required value or behavior |
| --- | --- |
| `Identity:Issuer` | Exact public HTTPS origin with trailing `/`, no path, credentials, query or fragment. It must match discovery, client configuration and verifier trust. |
| `Identity:SecretStore:Path` | Read-only secret-store mount containing realm signing/encryption pairs and the Data Protection ring. Keys must be dedicated to this realm, not shared with another issuer. |
| `Identity:MongoDB:ConnectionString`, `Identity:MongoDB:Database` | Explicit operational-store connection and database. Use a transaction-capable replica set with durable writes, not a standalone development server. |
| `Cratis:Chronicle:ConnectionString` | Explicit native Chronicle client connection (environment form `Cratis__Chronicle__ConnectionString`). `Cratis:Chronicle:EventStore` defaults to `IdentityProducts`; isolate it from other realms. `ConnectionStrings:chronicle` is read only by the reference `Identity.Host` and has no effect in this image. |
| `Identity:FenceLog:ConnectionString`, `Database`, `InstanceId` | Independently provisioned fence-log custody. Its connection string must differ from the operational connection. Merely changing the URI or database name does not establish deployment or backup independence. |
| `Identity:FenceLog:MinimumSequence` | Explicit nonnegative, externally observed allocation bound. A fresh, independently initialized empty custody may start at `0`; never reset an existing custody's bound to make a restore pass. |
| `Identity:Accounts:LookupKey` | Secret lookup key: standard base64 decoding to at least 32 bytes (see [Lookup key encoding](#lookup-key-encoding)); keep its revision and custody stable across replicas and restarts. |
| `Identity:Accounts:ConfirmNoLegacyMongoStore` | Defaults to `false`. Leave the legacy-account guard enabled unless an operator has confirmed no legacy store needs preservation. This is not an automatic bootstrap flag. |
| `Identity:GitHub:ClientId`, `Identity:GitHub:ClientSecret` | Provisioned GitHub OAuth application, with its callback at the public issuer's `/signin-github`. Supply the secret through the platform secret mechanism. |
| `Identity:SecurityOwners:0` (and further indexes) | At least one explicitly provisioned issuer subject. The image never manufactures an owner, account or administrator on startup. |
| `Identity:Resources:<index>:TenantsCallback:Url`, `Secret` | Authenticated product callback: HTTPS without credentials or fragment and a shared secret of **at least 32 characters**. Required for each Direct entry. |
| `Identity:Tokens:AccessTokenLifetime` | Positive, at most `00:05:00`; shipped default is five minutes. The hosted-shared limit is deliberately stricter than the general access-token v1 ceiling. |
| `ForwardedHeaders:KnownProxies:<index>` | Explicit ingress IP addresses trusted to forward the client address and scheme. |
| `ForwardedHeaders:KnownNetworks:<index>` | Optional trusted ingress CIDRs such as `10.0.0.0/8`, narrowly scoped to the controlled ingress network. The CIDR form binds from configuration and environment (`ForwardedHeaders__KnownNetworks__0`) and is covered by a specification. An invalid address or CIDR throws when the forwarded-headers options are built. Loopback defaults remain. Never use all-address networks. |
| `Identity:IntrospectionClients:<index>:ClientId`, `ClientSecret` | Optional; only for a product verifier that uses introspection. Both are required per entry and client ids must be unique. The shipped list is empty. See [Indexed collections](#indexed-collections). |

For environment injection, the fence-log variables are `Identity__FenceLog__ConnectionString`, `Identity__FenceLog__Database`, `Identity__FenceLog__InstanceId` and `Identity__FenceLog__MinimumSequence`. These bind the runtime's `Identity:FenceLog` section; there is no `Revocation` nesting.

Startup aggregates missing or invalid hosted-shared settings into one value-free diagnostic list before normal Identity storage setup. Existing legacy-certificate, secret-store, account, resource and token checks still apply. An invalid or inaccessible key mount, invalid MongoDB connection, missing custody or uncertain recovery continues to fail closed; validation does not provision or prove any of these dependencies.

Only loopback proxies are trusted by default. Configure the actual ingress addresses before using TLS termination. Forwarded headers run before Identity, so only a trusted ingress can establish the external HTTPS scheme. Restrict direct access to port `8080`; the ingress must discard caller-supplied forwarded headers and provide its own. Configure host routing and `AllowedHosts` for the intended public origin. Do not make arbitrary forwarded hosts an issuer authority.

### Chronicle TLS trust

The Chronicle client connects with TLS and client credentials in the connection string (`chronicle://<client>:<secret>@<host>:35000/`). In `Cratis.Chronicle` 19.111.0, where this behavior was checked, server-certificate validation is controlled by the connection-string query parameter `skipTlsValidation`, which **defaults to true**: any server certificate is accepted, including a private-CA or self-signed one. As checked at that version, the image therefore connects to an internal-CA Chronicle, but **without server authentication**. Do not claim Chronicle server authentication in the D13 gate while this default is in effect. The client is now pinned to 19.160.0 in `Directory.Packages.props`; this default has not been re-verified at that version, so treat it as still in effect until it is. The integration kernel image (`cratis/chronicle:19.144.0-development` by default) is selected independently of the client pin.

With `skipTlsValidation=false` the client validates the chain against the .NET default (operating-system) trust store. The client has no option for a custom CA file; the only other accepted case is a server certificate whose hash matches a configured client certificate, which is not a CA-trust mechanism. To trust a private CA with validation on, Linux .NET honors `SSL_CERT_FILE` or `SSL_CERT_DIR` (OpenSSL store): mount the internal CA PEM read-only and set the variable. Such a variable replaces the default roots for the whole process, so the bundle must also contain every public root the process needs, or use a hashed `SSL_CERT_DIR` that adds the CA. **Not verified in the image; prove it with a staging connect smoke.**

Recommendation: in staging, the default is acceptable only while network policy restricts Chronicle ingress to the Cratis ID pods on its port; record it as an accepted risk. In production use `?skipTlsValidation=false` plus the CA bundle. The connection string is a secret: show only redacted templates and keep it in the secret mechanism.

### Indexed collections

Configuration arrays are addressed by index, and environment variables merge into the shipped `appsettings.json` arrays **by index**. For `Identity:Resources` the shipped order is a contract: index `0` is `https://app.cratis.direct/v1` (audience `direct-v1`) and index `1` is `https://app.cratis.direct/mcp` (audience `direct-mcp`). Index `2` is Studio (audience `studio-mcp`). Infrastructure sets `Identity__Resources__<N>__TenantsCallback__Url` and `__Secret`, plus `Identity__Resources__2__Indicators__0` for Studio. Reordering or inserting a shipped entry silently re-targets those secrets, so any further entry must be appended (index `3` onward) and the automation must pair index and secret.

`Identity__IntrospectionClients__<N>__ClientId` and `__ClientSecret` are both required for each entry, and client ids must be unique; the shipped list is empty. Use contiguous indexes starting at `0`. Validation enforces only that the secret is present; use a high-entropy value of at least 32 characters, as for callbacks.

Authenticated, authorized introspection of active OAuth grants now includes `credential_kind` (`oauth-grant`), the non-secret grant event-source UUID `credential_id`, token-derived `tid`/`aud`/`scope`, and the current recovery `cache_epoch`. Code-redemption/refresh JWTs carry the same UUID as advisory access-token-only `cid`. Missing authorization means no grant credential pair or `cid`; invalid/expired/revoked tokens return only `active: false`, and failed authentication or unproven recovery exposes no ids. Consumers invalidate cached results on epoch changes; these fields do not supply product user/tenant keys, roles, or permissions. Product-owned resolution remains required. **Second step, pass-through only:** well-formed token `cnf` is preserved by OpenIddict, not invented or proof-validated here; bearer tokens have none until DPoP issuance lands. Malformed `cnf` errors before credential ids are attached. See [Resource token validation](../security/resource-token-validation.md).

### Secrets as configuration

No key-per-file configuration provider is registered. Secrets reach settings through environment variables (for example Kubernetes `secretKeyRef`) or the platform's configuration; the read-only file mount holds only the key store (`Identity:SecretStore:Path`: realm keys and the Data Protection ring). Secret-bearing settings are `Identity__MongoDB__ConnectionString`, `Identity__FenceLog__ConnectionString`, `Cratis__Chronicle__ConnectionString`, `Identity__Accounts__LookupKey`, `Identity__GitHub__ClientSecret`, `Identity__Resources__<N>__TenantsCallback__Secret` and `Identity__IntrospectionClients__<N>__ClientSecret`. Environment values are visible to the process environment and crash dumps; never log them.

### Operational MongoDB permissions

Before normal storage setup, the legacy-account guard lists collections named `accounts` in `Identity:MongoDB:Database` and reads at most one `_id`. The operational user therefore needs at least `listCollections` and `find` (the built-in `read` role) on that database from the first start. Normal operation additionally needs write access and index creation there (`readWrite` covers both; startup creates the unique and TTL indexes) and a replica set for transactions. If the read is refused (MongoDB error 13 or 18), the guard fails closed as an authentication failure; do not set `ConfirmNoLegacyMongoStore` as a workaround. The fence-log connection is separate and uses its own credentials and database.

### Lookup key encoding

`Identity:Accounts:LookupKey` must be **standard base64** that decodes to at least 32 bytes. Outside Development, options validation rejects a value that is not base64 or decodes to fewer bytes, with the value-free message "Account lookup key must be base64 with at least 256 bits of key material." A plain password is not accepted: a 32-character password decodes to 24 bytes, and base64url input is not standard base64. Generate 32 random bytes, encode them with standard base64 without a newline, and give every replica the same value; keep `Identity__Accounts__LookupKeyRevision` at `1`.

### Open decisions

These are not changed by this document and belong to L0:

- **Key-based collections (M7):** not added. Index addressing is the configuration model; a keyed form would change the public options shape and validation. Recommended cheap mitigation: a specification pinning the shipped resource order.
- **Key-per-file source (M11):** not added. It adds a package and lets a mount override any setting. Recommended: keep `secretKeyRef`; if wanted, a separate change that loads it only from a directory named by one variable, with precedence and trailing-newline specifications.
- **Passphrase lookup key (M14):** not added. Recommended: keep base64 only and have the Infrastructure automation encode; a passphrase form would freeze key-derivation parameters into every stored value. A cheap follow-up is naming the setting and shape in diagnostics.

## Token endpoint rate limits

`Identity:TokenRateLimit` bounds `/connect/token` admission before realm processing and client authentication. Matching follows OpenIddict 7.8.0's URI canonicalization, including `PathBase`, case-insensitive paths, dot segments and a trailing slash; ambiguous URIs are limited rather than bypassed. Environment variables use `Identity__TokenRateLimit__<setting>`.

| Setting | Default | Accepted value |
| --- | --- | --- |
| `Enabled` | `true` | Set `false` to bypass all admission counters. |
| `Window` | `00:01:00` | Positive duration, at most one hour. |
| `PerSourceRequests` | `300` | Positive integer; IPv4 address or IPv6 /64 budget. |
| `PerClientRequests` | `120` | Positive integer; claimed client id budget within a source, across grants. |
| `PerClientExchangeRequests` | `30` | Positive integer; additional claimed-client token-exchange budget within a source. |
| `MaxTrackedKeys` | `10000` | Integer from 100 through 1,000,000; separate capacity for each key kind. |

Validation is skipped when disabled. Counters are local to each process, reset on restart and are not shared by replicas. The source budget is authoritative: it is charged first, and a source refusal neither allocates nor charges client/exchange counters. Client refusals do not charge exchange counters. All admission decisions and charges occur under one lock. A claimed client id is unauthenticated, so client/exchange budgets are scoped to the normalized source, never shared across sources. Forging a victim's id cannot consume its budget at another source; rotating ids never bypasses the source budget. Clients sharing egress still share that source budget by design.

Source, client and exchange tables each retain at most `MaxTrackedKeys` entries; client/exchange keys include the source. At capacity, live keys are retained, never evicted. New client/exchange keys share fallback counters owned by their retained source, so filling one table cannot exhaust another kind's capacity or another source's fallback. A full source table uses one shared source overflow budget for untracked sources (with shared client/exchange fallbacks): distributed source exhaustion can therefore reduce availability for new sources, but cannot evict established sources or turn new sources into an unlimited bypass. Each retained source owns two fixed fallback counters, plus three process-wide overflow counters: at most five times `MaxTrackedKeys` plus three counters in total. Expired entries are reclaimed, retaining source fallback state until its latest window expires. Missing, malformed or oversized client ids still consume the source budget. GET requests consume only the source budget. IPv4-mapped IPv6 is normalized to IPv4; native IPv6 is grouped by /64.

Only `Connection.RemoteIpAddress` is used. The limiter never reads forwarding headers: configure forwarded-headers middleware **before** `MapCratisIdentity` with the explicit trusted proxies/networks above. Untrusted headers do not select a source partition.

A refusal returns HTTP 429 and OAuth `temporarily_unavailable` with the generic description "Too many requests. Try again later." Responses are not cacheable and `Retry-After` is the remaining window of the first refusing budget, rounded up to integer seconds (minimum one). Refused traffic does not extend a window; retrying may encounter another exhausted budget. Clients should wait at least that long. Warning logs are throttled to one per reason per window; the next warning reports the previous window's suppressed refusals. Client references are truncated hashes of tracked ids, otherwise `unknown`; tokens, secrets and raw claimed ids are not logged.

Tune budgets for shared egress and client traffic; keep edge-level rate limits. This in-memory admission control is not a distributed DoS defense and adds no database, Chronicle or Redis storage.

## Secret mount and state ownership

Use the [key-file contract](../../README.md#keys-and-the-secret-store): `signing.<name>.crt/.key`, `encryption.<name>.crt/.key` and `data-protection.<name>.xml`. The server reads and reloads this directory; it does not create keys or persist a replacement ring. All replicas of one realm need consistent active key versions and ring custody. Keep staging and production keys separate. Rotate and recover keys through the platform secret mechanism, not an image rebuild.

The [storage contract](../design/identity-package-and-storage-v1.md#2-store-inventory) remains authoritative:

| Store | Deployment responsibility |
| --- | --- |
| Chronicle | Durable domain facts, isolated event store and projections; never token-consumption authority. |
| Operational MongoDB | Token/authorization state, sessions, transactions, fences, outbox and checkpoints; never private key custody. |
| Independent fence log | Independently initialized custody metadata, monotonic sequence and contiguous coverage; **outside both Chronicle and operational backup sets**. Never restore it backward with either store. |
| Secret store | Realm keys, Data Protection, lookup and upstream secrets with restricted access and separate recovery/rotation procedures. |

Initialize fence custody and record its `InstanceId` and allocation bound out of band before first startup. The image does not create independent-custody metadata or declare recovery proven. Restore declarations and restriction reestablishment remain the existing recovery protocol's responsibility; no host flag overrides the recovery barrier. Preserve tombstones, denial fences and erased-key exclusions when recovering either data or secrets.

## Resource catalog and callbacks

The shipped catalog has two Direct entries and the Studio entry:

| Resource indicator | Audience | Scope names |
| --- | --- | --- |
| `https://app.cratis.direct/v1` | `direct-v1` | `direct:read`, `direct:content.write`, `direct:work` |
| `https://app.cratis.direct/mcp` | `direct-mcp` | `direct:mcp:read`, `direct:mcp:content.write`, `direct:mcp:work` |
| Supplied by the environment (see below) | `studio-mcp` | `studio:mcp:read`, `studio:mcp:write` |

Resources have no fixed tenants. **Direct must expose a tenants callback with a shared secret of at least 32 characters, and so must Studio when it is enabled**; authentication to the callback does not replace the product's live membership checks. Direct must configure a callback for both resource entries, even when both use the same product endpoint. Product owners supply callback URLs and secrets. The image does not implement product callbacks.

**Studio (index `2`) is optional.** The image ships the audience, display name and scopes but an **empty indicator list**, because staging and production Studio hosts differ and a hostname is never hardcoded in the image. The image starts without Studio:

- **Entirely unconfigured** (no indicator, no callback URL and no callback secret): `studio-mcp` is **disabled**. It is removed from the effective resource catalog, so a token or authorization request for that audience is refused like any unknown resource and the consent and discovery surfaces do not advertise it. The host logs one value-free line at startup ("Optional resource studio-mcp is not configured and is disabled"). Options reload cannot enable it; restart the host after configuring it.
- **Partially configured** (any of the three present, not all valid): the host refuses to start, naming `studio-mcp`.
- **Fully configured:** `studio-mcp` is served with these settings.

| Setting | Requirement when Studio is enabled |
| --- | --- |
| `Identity__Resources__2__Indicators__0` | The HTTPS resource indicator of that environment's Studio MCP endpoint. |
| `Identity__Resources__2__TenantsCallback__Url` | HTTPS, no user info or fragment. |
| `Identity__Resources__2__TenantsCallback__Secret` | At least 32 characters; secret-bearing, never logged. |

Only `studio-mcp` is optional; the Direct entries stay mandatory. For hosted-shared, **every** resource indicator, Direct's included, must be an absolute HTTPS URI; the refusal names the resource, never the value. The callback follows the same contract as the Direct callbacks above; tenant ids it returns must be lowercase. There is no placeholder indicator in this image.

Client and scope materialization reads configuration at each startup. The configured clients retain stable client ids; restarting reconstructs the same catalog instead of issuing registration commands or accumulating registrations. The CLI descriptor remains public-client/PKCE constrained. The host does not depend on the registration-command work in #39 and does not bootstrap an administrator.

## Health and readiness

| Endpoint | Meaning | Probe use |
| --- | --- | --- |
| `/healthz` | Process liveness, independent of recovery readiness. | Liveness; do not restart a live process merely because reconciliation is incomplete. |
| `/readyz` | Only the registered `identity-restriction-recovery` health check: HTTP 503 while the recovery barrier is closed or no matching check is registered, HTTP 200 after recovery is proven. | Readiness; do not route issuance traffic while unhealthy. |

The Cratis ID host explicitly maps `/healthz` and `/readyz`; `MapCratisIdentity()` owns neither probe nor a root banner. The host calls `UseCratisIdentity()` for routing, realm scope, security headers and authentication, and opts in to the antiforgery-protected `/logout` form with `MapSignOut = true`. A configured `Identity:Endpoints:RoutePrefix` moves protocol and sign-out routes, not these probes, discovery/JWKS or `/signin-github`.

Readiness is a protocol barrier, not a claim that production approval or D13 evidence exists. A healthy process can still deny individual operations. Offline access tokens remain bounded by their lifetime and verifier policy; closing readiness does not instantly revoke already issued JWTs.

## Staging and production gates

**All D13 evidence gates must close before any products-realm production issuance.** The same image may serve a staging end-to-end exercise before those evidence gates close. Infrastructure must not route the production products-realm hostname or production issuance traffic to it until the gates are closed. There is no code or image-build wait on those evidence records.

| Gate | Staging end-to-end | Production products-realm issuance |
| --- | --- | --- |
| Dedicated realm keys, valid key mount, access token lifetime at most five minutes | Required | Required |
| External Chronicle, operational replica set, independently initialized fence custody, account lookup key, explicit owner and upstream app | Required | Required |
| Trusted ingress, exact issuer, authenticated product callbacks, no fixed tenants, recovery readiness | Required | Required |
| Tested emergency key rejection surviving restore | Evidence may be gathered in staging | Evidence closed before issuance |
| Grant and session invalidation after key rejection | Evidence may be gathered in staging | Evidence closed before issuance |
| Rotation rehearsed in staging | Exercise here | Evidence closed before issuance |
| Per-product audience/key binding enforced by verifiers | Verify in the staging consumers | Evidence closed before issuance |
| Published `identity-access-token-v2` and `identity-revocation-v2` contracts | Not required to build or exercise this image | Required before issuance |

These are deployment prerequisites, not features newly implemented by this host. In particular, emergency rejection/retirement tombstones, restore-safe rejection, grant/session invalidation and recovery protocol changes remain outside this image slice. Do not treat a successful build, image publication or `/readyz` response as D13 evidence closure.

## End-to-end prerequisites

Before a staging login/consent/token exercise, provision the issuer DNS/TLS and restricted ingress; all four storage/custody boundaries; the GitHub callback; a known security-owner subject; Direct's two authenticated tenants callbacks; and product/CLI issuer, audience and scope trust. Provide the host UI needed for interactive account, tenant and consent flows separately: this image adds no new UI or UI-conformance preset. Product verifiers must enforce exact issuer/audience, scope and live tenant membership; a callback selection alone is not authorization.

### Image smoke

The integration project includes a container smoke, opt-in with `CI=true` and `CRATIS_ID_IMAGE_SMOKE_ENABLED=true`; without both it is skipped, so normal and local runs stay green. It needs a local Docker daemon and the image already loaded as `CRATIS_ID_IMAGE_SMOKE_IMAGE` (default `cratis-id:ci`). The `smoke` job in the image workflow runs it for every pull request and main push after `build` succeeds: it rebuilds the `cratis-id` image with `load: true` (no registry sign-in, no push, `contents: read` only) and runs the two specs below.

- `when_starting_the_cratis_id_image` provisions throwaway infrastructure on a private Docker network: an operational MongoDB replica set, a separate fence-log MongoDB with an empty custody document, a pinned Chronicle development kernel and a read-only secret-store mount with freshly generated realm keys and Data Protection key. All settings are injected as environment variables with run-time generated secrets and nothing is committed. It asserts `/healthz` and `/readyz` both return 200.
- `when_starting_the_cratis_id_image_without_configuration` runs the image with no required settings except secret-bearing ones and asserts it exits non-zero without echoing the supplied secret or a connection string.

To run it locally, on a machine with Docker:

```bash
docker build -f Docker/Dockerfile --target cratis-id --build-arg HOST=Identity.CratisId -t cratis-id:ci .
CI=true CRATIS_ID_IMAGE_SMOKE_ENABLED=true ./Source/Identity.Integration.Specs/bin/Release/net11.0/Cratis.Identity.Integration.Specs \
  -class Cratis.Identity.Integration.for_Host.when_starting_the_cratis_id_image \
  -class Cratis.Identity.Integration.for_Host.when_starting_the_cratis_id_image_without_configuration
```

Set `IDENTITY_CHRONICLE_IMAGE` to override the pinned Chronicle image. The smoke proves the image starts, reconciles and answers its probes against real stores; it does not prove GitHub sign-in, since the upstream credentials are placeholders.

### Publish checklist

The image is published by every release. Before relying on a release, a maintainer:

1. Confirms the `smoke` job is green on the main commit to publish.
2. Merges a pull request labelled `patch`, `minor` or `major`, then records the published digest for deployment, which must pin it.

A container smoke is not a full protocol or D13 rehearsal. Browser sign-in, tenant selection, consent, refresh, product verifier behavior and restore/rotation exercises need their owning fixtures and operational procedures. Infrastructure manifests, rollout/rollback policy, per-app/self-hosted presets, bootstrap/upgrade tooling and host UI conformance are separate work.

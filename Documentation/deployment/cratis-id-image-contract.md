---
title: Cratis ID image contract
description: Configure the hosted-shared Cratis ID image, its storage and ingress boundaries, and its staging and production prerequisites.
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Cratis ID is the hosted-shared Identity server for the products realm. `Source/Identity.CratisId` builds `Cratis.Id.Host.dll`; `Identity.Host` remains the development/reference host. This contract gives infrastructure owners the image, configuration and readiness boundary, not deployment manifests or permission to issue production credentials.

The **same image serves staging and production**. Run both with `ASPNETCORE_ENVIRONMENT=Production`, distinct issuer origins, realm keys, accounts and stores. Do not use Development to bypass startup requirements in staging. The single `Identity:Hosting:Preset` is `hosted-shared`; per-application and self-hosted presets are deferred.

## Image and process

The working image name is `ghcr.io/cratis/id`, isolated to the image workflow so it can be renamed. The workflow builds pull requests without signing in to a registry or pushing. Publication is **off by default**: only a main-branch or `v*` tag push with repository variable `CRATIS_ID_IMAGE_PUBLISH_ENABLED=true` enters the publishing job. Only that job requests `packages: write` and uses `GITHUB_TOKEN`. It publishes a commit-SHA tag and either `latest` on main or the release tag. Deployment should select an immutable digest; publishing an image does not deploy it.

Build the hosted image from the repository root:

```bash
docker build -f Docker/Dockerfile --target cratis-id --build-arg HOST=Identity.CratisId -t cratis-id:local .
```

Both arguments matter: `HOST` selects the project to publish; `--target cratis-id` selects its entrypoint. Without them, the final `reference` target and `Identity.Host` project preserve local compose behavior.

The runtime is .NET 10 ASP.NET Core, chiseled and non-root, listening on HTTP port `8080`. It has no shell or package manager and contains no realm keys, upstream credentials or provisioned databases. Mount secrets read-only with permissions readable by the image's non-root user. No Docker socket, privileged mode or container-managed MongoDB/Chronicle is part of the server contract.

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

The Chronicle client connects with TLS and client credentials in the connection string (`chronicle://<client>:<secret>@<host>:35000/`). In the pinned client (`Cratis.Chronicle` 19.111.0) server-certificate validation is controlled by the connection-string query parameter `skipTlsValidation`, which **defaults to true**: any server certificate is accepted, including a private-CA or self-signed one. The image therefore connects to an internal-CA Chronicle today, but **without server authentication**. Do not claim Chronicle server authentication in the D13 gate while this default is in effect.

With `skipTlsValidation=false` the client validates the chain against the .NET default (operating-system) trust store. The client has no option for a custom CA file; the only other accepted case is a server certificate whose hash matches a configured client certificate, which is not a CA-trust mechanism. To trust a private CA with validation on, Linux .NET honors `SSL_CERT_FILE` or `SSL_CERT_DIR` (OpenSSL store): mount the internal CA PEM read-only and set the variable. Such a variable replaces the default roots for the whole process, so the bundle must also contain every public root the process needs, or use a hashed `SSL_CERT_DIR` that adds the CA. **Not verified in the image; prove it with a staging connect smoke.**

Recommendation: in staging, the default is acceptable only while network policy restricts Chronicle ingress to the Cratis ID pods on its port; record it as an accepted risk. In production use `?skipTlsValidation=false` plus the CA bundle. The connection string is a secret: show only redacted templates and keep it in the secret mechanism.

### Indexed collections

Configuration arrays are addressed by index, and environment variables merge into the shipped `appsettings.json` arrays **by index**. For `Identity:Resources` the shipped order is a contract: index `0` is `https://cratis.direct/v1` (audience `direct-v1`) and index `1` is `https://cratis.direct/mcp` (audience `direct-api`). Infrastructure sets only `Identity__Resources__<N>__TenantsCallback__Url` and `__Secret`. Reordering or inserting a shipped entry silently re-targets those secrets, so a later Studio entry must be appended (index `2`) and the automation must pair index and secret.

`Identity__IntrospectionClients__<N>__ClientId` and `__ClientSecret` are both required for each entry, and client ids must be unique; the shipped list is empty. Use contiguous indexes starting at `0`. Validation enforces only that the secret is present; use a high-entropy value of at least 32 characters, as for callbacks.

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

The shipped catalog has **Direct entries only**:

| Resource indicator | Audience | Scope names |
| --- | --- | --- |
| `https://cratis.direct/v1` | `direct-v1` | `direct:read`, `direct:content.write`, `direct:work` |
| `https://cratis.direct/mcp` | `direct-api` | `direct:mcp:read`, `direct:mcp:content.write`, `direct:mcp:work` |

Resources have no fixed tenants. **Direct and Studio must each expose a tenants callback with a shared secret of at least 32 characters**; authentication to the callback does not replace the product's live membership checks. Direct must configure a callback for both resource entries, even when both use the same product endpoint. Product owners supply callback URLs and secrets. The image does not implement product callbacks.

**TODO — Studio, assigned later by L103:** add the reviewed Studio indicator, audience and scopes only after the product owner supplies its registration and callback. There is no placeholder Studio resource or invented hostname in this image.

Client and scope materialization reads configuration at each startup. The configured clients retain stable client ids; restarting reconstructs the same catalog instead of issuing registration commands or accumulating registrations. The CLI descriptor remains public-client/PKCE constrained. The host does not depend on the registration-command work in #39 and does not bootstrap an administrator.

## Health and readiness

| Endpoint | Meaning | Probe use |
| --- | --- | --- |
| `/healthz` | Process liveness, independent of recovery readiness. | Liveness; do not restart a live process merely because reconciliation is incomplete. |
| `/readyz` | Only the registered `identity-restriction-recovery` health check: HTTP 503 while the recovery barrier is closed or no matching check is registered, HTTP 200 after recovery is proven. | Readiness; do not route issuance traffic while unhealthy. |

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

The integration project includes a CI-only image smoke scaffold. It is opt-in with `CI=true` and `CRATIS_ID_IMAGE_SMOKE_ENABLED=true`, a locally available `CRATIS_ID_IMAGE_SMOKE_IMAGE` (default `cratis-id:ci`), and `CRATIS_ID_IMAGE_SMOKE_CONFIGURATION_DIRECTORY` containing `appsettings.json` and readable test realm keys. The directory is mounted read-only at `/configuration`; its settings must reference isolated, reachable, pre-provisioned test stores and proven custody. The scaffold starts only the server image and checks liveness and reconciled readiness. It does not provision MongoDB, Chronicle or keys, and the image workflow does not enable it automatically.

A container smoke is not a full protocol or D13 rehearsal. Browser sign-in, tenant selection, consent, refresh, product verifier behavior and restore/rotation exercises need their owning fixtures and operational procedures. Infrastructure manifests, rollout/rollback policy, per-app/self-hosted presets, bootstrap/upgrade tooling and host UI conformance are separate work.

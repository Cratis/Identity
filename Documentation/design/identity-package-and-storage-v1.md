# Identity package, storage and host UI design

```text
Doc id: identity-package-and-storage
Version: 1
Status: Signed off
Signed off: 2026-11-05 by woksin, https://github.com/Cratis/Identity/issues/14; owner confirmation pending
Issue: https://github.com/Cratis/Identity/issues/27
Depends on: https://github.com/Cratis/Identity/issues/16 (D18)
Linked from: https://github.com/Cratis/Identity/issues/11
```

## 1. Scope and authority

This W0 design guides W2's package extraction and storage adapters. The headless module direction is decided; **D18 was decided as option A in #16**. This note records that decision, not an approved storage technology or implemented consistency guarantee. The note was signed off in Identity #14 (2026-11-05). The header records the intended #11 linkage; linking and merge remain follow-up work.

The [Terms of identity-contract-v1](../specs/identity-contract-v1.md#terms) apply unchanged. The [event rules and catalogue](../specs/identity-events-v1.md) determine which facts are events, their public/internal visibility, generations, privacy and secret exclusions. The [revocation protocol](../specs/identity-revocation-v1.md) owns fence durability, consumption concurrency, recovery and readiness; it takes precedence over this summary. All three specifications are signed off; owner confirmation is pending in Identity #14.

## 2. Store inventory

These are logical responsibilities, not permission to co-locate every store in one database or backup set. Each realm has its own Chronicle event store and secret/key boundary. References to products' credential authorities do not transfer their ownership to Identity.

| Store | Contents | Writer | Readers | Consistency | Backup/restore rule | Retention/PII |
|---|---|---|---|---|---|---|
| Chronicle event store | Only catalogue facts: accounts and links, lifecycle/erasure, clients and secret-rotation metadata, resources/scopes, consent/grants and family lifecycle, signing-key references, realm epochs and D5-dependent Access facts. No individual token rotations, sessions or redeemable values. | Identity commands; Access commands for its catalogue facts; outbox dispatcher for fenced facts. | Identity projections, approved public-event consumers, audit. | Guarded domain appends; stable ids/generations and keyed outbox publication. Not the serialization point for token use. | Preserve realm, generations, append identities and recovery metadata. Reconcile against independent fences and erasure fences before readiness; event replay alone cannot prove recovery. | Durable history with first-generation PII protection and end-to-end erasure per event rules. Only reviewed non-redeemable digests/references, never secrets. |
| Operational store | OpenIddict token records: authorization/device/user codes and individual refresh rotations; consumption compare-and-set (CAS), grant/family lineage, authorizations' live status, account/grant/family/realm fences and epochs, tombstones, outbox and checkpoints. OpenIddict application/scope/authorization materializations contain non-secret configuration, approved digests and references, not a second configuration writer. Browser sessions and continuation status, pending consent/approvals, tenant selection and on-behalf session state; `state`/nonces, `jti` replay entries, CIMD fetch cache, rate counters; protected link evidence by non-redeemable EvidenceId. | Package commands, protocol handlers, recovery and idempotent configuration materializers. | Package verifiers, OpenIddict adapters, recovery and dispatcher; authorized evidence reviewers. | Durable replicated serialization/conditional transactions across use/consume/replace, applicable ancestors/fences/epochs and outbox. TTL and process-local locks are not concurrency guarantees. Materializations cannot supply positive live status. | Snapshot incarnation and contiguous fence coverage. Restore must reconcile fences **and** invalidate uncertain consumption state under revocation §6 before readiness; an old code, refresh token or approval must not become usable again. | TTL for ephemeral state, bounded caches and protected evidence retention; consumed refresh records retained for reuse detection. Tombstones outlive revoked credentials/grants and backup windows. Minimize/encrypt personal state; no signing/encryption keys or raw client secrets. |
| Independent fence log | Immutable scoped fences/epochs, sequences and enough non-secret outbox intent for reconstruction; independent recovery watermarks/coverage. | Credential authority's revoke/recovery protocol. | Recovery, outbox reconciliation and durability monitoring. | Independent durable idempotent append, monotonic allocation and contiguous coverage, as specified by revocation. | **Outside both Chronicle and operational-store backup sets.** Independently recoverable; never restored backwards with them. Recovery requires the independent recovery epoch and complete restriction recovery, with no exception; missing coverage keeps affected classes denied. | Fences/intent outlive the longest revoked credential/grant and both stores' backup/restore windows. Minimal pseudonymous denial identities, no redeemable material. |
| Secret store / KMS | Realm signing and encryption keys, protected Data Protection key ring, client secrets where retained, upstream credentials, digest keys and PII key custody. | Authorized key/secret lifecycle operations through restricted adapters; operator provisioning. | Only authorized package cryptographic operations; public verification material exposed through the protocol, not private keys. | Durable, access-controlled key versions and rotation; key loss/unavailability fails closed outside explicit development fixtures. | Separate protected key recovery and rotation drills; enforce erasure fences/key purge before using restored keys. Never restore erased subject keys into service. | Private keys and raw client secrets **never enter events or the operational store**, including encrypted copies. Purpose-bound versions; minimize retained secrets and purge per reviewed erasure/rotation policy. |
| Protected credential store | Server-held passkey (WebAuthn) verification material referenced by `PasskeyRegistered`: credential id, public key (COSE), sign counter, transports, attestation result and AAGUID, bound to the account subject. Authenticator-held private keys never leave the authenticator and are never stored anywhere in Identity. Events carry only the reference/thumbprint (identity-events-v1 §6.1). | Package passkey registration, sign-in (counter update) and removal commands. | Package passkey verifier only; account owner's credential-management queries see non-sensitive metadata. | Durable conditional writes per credential (counter monotonicity, removal). A removed or revoked credential is denied even if a projection still lists it. | Restored records MUST NOT revive a removed credential: reconcile against account fences/tombstones and removal facts before readiness; a counter that went backwards after restore is treated as a cloned-authenticator signal and the credential is suspended. | Retained while registered; deleted on removal or account erasure (erasure fence). Pseudonymous keys only; no PII beyond the account reference. Logical store; may share infrastructure with the operational store if access and backup rules above hold. |
| Read models / projections | Account/profile views, connected apps, client/resource configuration views and other query models derived from facts. | Chronicle projections/reducers; idempotent materializers. | Package queries and authorized host UI; approved consumers. | Eventually consistent and rebuildable. **Never on the revocation path** or authoritative for consumption. | Rebuild from retained generations with erasure applied; restoring a projection cannot restore a credential or clear a fence. | Protected/minimized PII; erase exports/caches as part of the event specification's workflow. No credentials or keys. |
| Host application's own stores | Product person mappings keyed by exact `(issuer, subject)`, product tenants, live membership/roles, application data and UI preferences. No issuer keys in customer application stores. | Host/product commands and membership authority, not Identity projections. | Product authorization and application queries; Identity only through an explicit authenticated product callback where needed. | Product owns membership-sensitive serialization; unknown membership denies. Identity does not infer membership from an event or token. | Product-owned recovery with membership/erasure reconciliation; cannot roll back Identity fences or mint grants. | Product retention/PII obligations and consumer erasure acknowledgements; no duplication of Identity secret custody. |
| Logs / metrics | Bounded failed-sign-in/security diagnostics and operational health, not attacker-driven domain events. | Package and host telemetry. | Authorized operators/security monitoring. | Not authorization or recovery authority. | Operator-managed; telemetry restoration cannot establish credential readiness. | Redact tokens, codes, cookies, secrets and URL query material; bounded, reviewed personal-data retention. |

OpenIddict's application, scope and authorization interfaces can be Chronicle-backed without turning every record into an event: commands own configuration/consent facts, adapters materialize configuration, and authoritative live status/consumption stays operational. No revocation or redemption may read those projections to decide whether a credential remains usable. Access tokens are not stored. A digest permitted by the event catalogue is not permission to store the original secret; client-secret verification uses approved digests or a restricted secret-store operation.

## 3. Revocation path

Revocation authenticates and authorizes the action, commits the authoritative operational fence/epoch and immutable outbox intent together, then durably appends to the independent fence log before acknowledging success. Chronicle publication is asynchronous and idempotent, and **the revocation path never reads a projection**. Credential use/consumption serializes against the same operational fences; missing state, unavailable authority or uncertain recovery denies. This does not promise instant revocation of an already issued offline JWT. See [revocation §§3–7](../specs/identity-revocation-v1.md#3-revoke-sequence-and-acknowledgement) for ordering, retries, ancestor checks, freshness, retention and restore/consumption recovery; this note adds no alternate protocol.

```text
revoke command -> authenticate/authorize
               -> operational transaction [fence/epoch + outbox]
               -> independent fence log [durable acceptance]
               -> acknowledge

log-confirmed outbox -> keyed Chronicle append -> query projections
credential use       -> authoritative operational status + consume/replace CAS
                        ordered against applicable fences (no projection)
```

## 4. Packages and host surface

Names and boundaries follow Usher's backend-module-plus-contracts shape, not a new standalone server product.

| Package | Responsibility |
|---|---|
| `Cratis.Identity` | Headless Arc commands/queries, OpenIddict protocol integration, domain rules, projections and reactors, adapter contracts and startup validation. No bundled pages. |
| `Cratis.Identity.Contracts` | Catalogue event types and required domain concepts. Both public and internal events follow event rules; only explicitly public facts are integration contracts. |
| `Cratis.Identity.Storage.<Provider>` | Selected operational, independent fence-log and secret-store adapters, potentially separate provider packages. Chronicle integration consumes the host's realm-scoped Chronicle configuration. Names/technology remain open; adapter availability is not proof of durability. |
| `@cratis/identity` | Generated command/query proxies. |
| `@cratis/identity.react` | Optional reusable host-rendered interaction components. Not a second consent authority; any alternative UI must pass the same contract. |

Illustrative, **non-normative** signatures, not today's API or a compatibility promise:

```csharp
TBuilder AddCratisIdentity<TBuilder>(this TBuilder builder,
    Action<IdentityOptions> configure) where TBuilder : IHostApplicationBuilder;
IServiceCollection AddCratisIdentity(this IServiceCollection services,
    Action<IdentityOptions> configure);
IEndpointRouteBuilder MapCratisIdentity(this IEndpointRouteBuilder endpoints);
```

`AddCratisIdentity(options)` registers dependencies and validates options at startup, with a builder wrapper delegating to service registration as in Usher. `MapCratisIdentity()` maps standard OAuth/OIDC endpoints for the supported profile and package interaction commands/queries. It does not silently enable a protocol feature. W2 must define middleware ordering with Arc/authentication and fail startup for missing required adapters or non-development keys. There is no UI-specific grant API bypass.

The application-facing surface is **only commands, queries and events**: register clients/resources, query a pending consent request, submit its decision, link an upstream key, list/revoke grants, and so on. Standards-defined discovery, JWKS, authorize, token, revoke and introspect endpoints are the protocol surface, not an exception allowing application code to mint credentials. Their state changes use the same package rules.

| Host supplies | Required boundary |
|---|---|
| Realm issuer and Chronicle | Exact configured issuer, own event store per realm, credentials/entitlements and privacy configuration; referenced module/contracts assemblies discoverable by Arc/Chronicle. |
| Operational store and independent fence log | Adapters proven for multi-replica serialization, ancestor contention, outbox idempotency, coverage and consumption recovery under the revocation spec; no in-memory production fallback. |
| Secret store / KMS | Realm-isolated signing/encryption keys, Data Protection custody and secret lifecycle/rotation. Preserve current fail-closed non-development startup. |
| UI pages | Login, consent and enabled device/link/account interactions on the issuer's own origin; render package view models, submit package commands, and pass §5 conformance. |
| Product callbacks and operation | Authenticated product tenant/live-membership integration, no configured membership authority outside development fixtures; monitoring, backup/restore and key drills. |

Embedding is not a trust boundary against the host process. Proposed hosted Stage identity runs outside untrusted customer application code; the products realm is not embedded in Direct or Studio. Compact/self-hosted embedding remains an operator choice. D18 is decided as option A (#16); D19 (Identity #17) is decided as option B: one platform-run Identity host serves customer Stage app realms, each realm with its own signing keys and stores and never a shared key store, with a dedicated host per app as option C. The products realm (D13, Identity #15) runs on a separate host, not the shared customer host.

## 5. Consent contract: checks for #48

These are observable checks for every host UI, including optional components. The **package owns the consent decision**. The host renders the package's validated request and returns the person's approve/deny intent; it cannot create authorization merely by asserting approval. #48 must exercise both the UI response and the resulting package state, including concurrent submissions.

1. **Package authority.** A host-created approval without a package-issued pending request fails and produces no grant or token. Every valid decision is revalidated by the package before any grant is committed.
2. **Immutable binding.** The package creates a short-lived single-use request bound to the exact browser session, client, scope set, resource and PKCE challenge/method (and validated redirect URI). Tampering with any binding, including omitting PKCE, fails without granting anything. Sensitive bindings are resolved server-side, not trusted from hidden fields.
3. **Anti-forgery.** A decision without a valid session-bound anti-forgery token fails; missing, altered and another session's tokens each produce no consent state change. GET rendering cannot approve.
4. **Frame denial.** Host-rendered consent and login responses carry both CSP `frame-ancestors 'none'` and `X-Frame-Options: DENY`. An attempted framed interaction cannot complete; a host omitting either header fails UI conformance.
5. **Untrusted presentation.** Client names, logos and URIs are untrusted. The package-provided view model exposes escaped text/attribute values and validated safe URI representations, never raw HTML; unsupported URI schemes are rejected or omitted. Injection fixtures in each field render as inert data, with no executable markup or script navigation. The host cannot opt into raw rendering.
6. **Render only.** A host submission cannot approve a different request, widen scopes, substitute a resource/client or bypass the person's decision. Added/changed permission fields fail rather than altering the stored request. Denial creates no grant. A hostile same-process host remains outside this UI contract's threat boundary.
7. **Single use and replay.** After either approval or denial, replay fails with no new grant, token or repeated effect. Two simultaneous decisions across replicas yield at most one successful consume/decision commit, ordered against session/grant fences.
8. **Expiry.** A decision at or after the package-recorded expiry fails, even if the page was rendered earlier. No grant or token is released.
9. **Session isolation.** A valid pending request submitted from another session, a signed-out session or a fenced session fails, even for the same account and a valid anti-forgery token in that other session.
10. **What is shown is granted.** For a successful approval, the scope set and resource displayed from the immutable package view model equal those on the committed grant; token issuance cannot exceed them. Changed/stale view bindings fail and require a new display/decision, not silent substitution.
11. **Interaction origin and warning.** Login/consent is served on the issuer origin and displays the package-provided “only continue if you started this” warning. A host moving the interaction to another origin or removing the warning fails conformance.

No choice of HTML framework establishes these checks by itself. Package validation, adapter concurrency tests and a host UI conformance suite are all required; this document is not evidence of passing #48.

## 6. Migration from the standalone server

Today's `Source/Identity/Program.cs` calls the configuration/environment overload of `AddCratisIdentity` and then `UseCratisIdentity()`. `Hosting/IdentityServiceCollectionExtensions.cs` wires stock OpenIddict MongoDB stores, account documents, client seeding and token pruning; `Hosting/IdentityEndpoints.cs` maps protocol handlers and bundled responses. `Configuration/IdentityOptions.cs` and related options hold issuer, the secret store, MongoDB, resources, tenants and token settings. `Keys/` reads the signing and encryption keys and the read-only Data Protection key ring from the secret store directory (`Identity:SecretStore:Path`), and reloads it so rotation needs no restart; `Storage/StorageIndexes.cs` and `Storage/TokenPruning.cs` manage current store behavior. These are inspected current-state facts, not the proposed guarantees.

W2 extracts the module/contracts and thin reference host, replaces bundled pages with host rendering, and moves registration/configuration mutations to commands. Account ids and exact realm scope must be preserved per contract v1; generic upstream links, uniqueness and quarantine need explicit implementation. Configuration views move behind Chronicle-backed adapters; live status/consumption moves to the proven operational boundary, with independent log/recovery and secret-store custody, including the Data Protection ring. Configured tenants become development fixtures plus a product-owned callback.

Do not copy active codes, stale consumption records or key-ring XML into events. If existing installations need cutover, reconcile accounts/configuration and deny uncertain credentials, requiring fresh authentication/consent rather than replay-minting tokens. Preserve non-development key checks; prove protocol, consent, multi-replica and restore conformance before deployment. This note changes no current API, configuration or runtime behavior.

## 7. Decisions recorded at sign-off

- **D18 (#16):** decided as option A: the event/operational/secret boundary, materialization and hosted-process details as described above. **D19 (#17):** decided as option B with option C: one platform-run Identity host for customer Stage app realms, each realm with its own signing keys and stores (never a shared key store) and a dedicated host per app as option C; separate stores do not contain a host compromise, so the host-compromise runbook (#18) covers recovery across all realms the host runs; the products realm runs on a separate host.
- **Package names:** module/contracts, adapter split, proxy/component names and reference-host ownership were signed off in Identity #14; signatures above are sketches.
- **Store technologies:** W2 must prove independent fence-log durability/retention, Chronicle keyed append/version compatibility and secret/KMS adapters. No database technology is selected here.
- **Review:** consent-view-model/host conformance and privacy/retention boundaries were signed off in Identity #14. #48 supplies executable consent evidence, not this document.

## 8. Reference

Inspected Usher source (paths relative to the Cratis repositories directory):

| File | Shape reused |
|---|---|
| `Showrunner/Source/DotNET/Usher/Usher.csproj` | Packable module referencing Arc, Arc.Chronicle and its contracts; command/query proxy generation. |
| `Showrunner/Source/DotNET/Usher/UsherHostApplicationBuilderExtensions.cs` | `AddUsher<TBuilder>(Action<UsherOptions>)` delegates to service registration. |
| `Showrunner/Source/DotNET/Usher/UsherServiceCollectionExtensions.cs` | `AddUsher(IServiceCollection, Action<UsherOptions>)`, dependency registration and startup options validation. |
| `Showrunner/Source/DotNET/Usher/UsherEndpointRouteBuilderExtensions.cs` | `MapUsher(IEndpointRouteBuilder)` maps package-owned endpoints. |
| `Showrunner/Source/DotNET/Usher/UsherOptions.cs` | Explicit host-supplied module options, not a standalone server configuration contract. |
| `Showrunner/Source/DotNET/Usher.Contracts/Usher.Contracts.csproj` | Separate contracts package with matching package/assembly identity for discovery. |

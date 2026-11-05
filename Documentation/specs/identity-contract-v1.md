# Person, account, resource, tenant and token contract

```text
Spec id: identity-contract
Version: 1
Status: Draft
Issue: https://github.com/Cratis/Identity/issues/24
Sign-off issue: https://github.com/Cratis/Identity/issues/14
Supersedes: None
```

## Terms

These definitions are normative within this Draft and MUST be reused verbatim by dependent W0 specifications. An identifier denotes identity, not authorization. “Detailed in” names the specification responsible for the remaining mechanics; it does not imply that specification is already Locked.

| Term | Definition | Identifier format if any |
|---|---|---|
| Person | The person-facing identity within a realm, identified by its exact realm-issued `(issuer, subject)` pair; not a claim of deduplication across realms. | Exact pair of issuer URL and subject UUID. |
| Account | Identity's realm-local record for a person, with an immutable subject, lifecycle state, linked upstream keys and protected profile. | `(issuer, subject)`; local id is subject. |
| Realm | An issuer trust boundary with its own accounts and signing keys, independent of process placement. | Configured realm issuer. |
| Issuer | The trusted authority issuing an identity or token; the realm issuer scopes a person subject, while an upstream issuer scopes a linked key. | Realm: exact HTTPS URL (§2.2); upstream: validated authority identifier. |
| Subject | The opaque, pseudonymous, immutable, never-reassigned account id issued within a realm; never email, login or upstream id. | Lowercase UUID v4 (§2.1). |
| Linked key | An immutable upstream identity associated with at most one account per realm, independent of mutable profile attributes. | Exact `(idp issuer, idp_sub)` string pair. |
| Resource | A registered protected API whose catalog entry binds a resource indicator, audience and scope prefix; detailed in Arc scope spec. | Registered resource indicator and audience. |
| Resource indicator | The absolute URL identifying the intended protected resource in an OAuth request, not a permission or an issuer. | Registered HTTPS URL (§4). |
| Audience | The resource identifier a token verifier MUST match to its own catalog entry; this profile permits one audience per access token. | Catalog string, 1–64 characters (§4). |
| Tenant | A product-owned workspace with product-owned live membership and roles; not an organisation or storage namespace. | Product/installation-scoped string, 1–64 characters (§4). |
| Organisation | Access's commercial and administrative unit for cross-product policy and confirmed tenant bindings, not workspace membership. | Opaque Access id; syntax open under D5. |
| Environment | A registered deployment context constraining a credential and tenant binding; never inferred from a caller-supplied label; detailed in Arc verified-caller spec. | Trusted installation reference; wire syntax open. |
| Actor | The current agent or supporter acting for the person, without replacing the person's subject; detailed in Arc delegation spec. | Registered actor `(iss, sub)` in `act`; namespaces open. |
| Credential kind | The verified credential classification determining validation and caller construction, not a role; detailed in Arc verified-caller spec. | Browser session, OAuth grant, PAT, on-behalf session, machine principal or workload. |
| Assurance (`acr`/`amr`) | Connection-mapped authentication strength (`acr`) and verified methods (`amr`), never authority inferred from untrusted upstream labels; detailed in Arc verified-caller spec. | `acr` string (`basic` if unknown); optional `amr` array of strings. |
| Policy epoch | A monotonically increasing organisation-policy revision forcing stale credentials to be re-evaluated against current policy; detailed in Arc verified-caller spec. | Nonnegative integer. |
| Access token | A short-lived resource-bound OAuth credential carrying a scope ceiling, not live membership; detailed in Arc scope spec. | Signed JWT with `typ=at+jwt` and claims in §5. |
| ID token | An OIDC authentication assertion to a client, never a resource access credential; detailed in Identity OIDC spec. | OIDC token profile; outside this contract's access-token schema. |
| Refresh token | A credential redeemable only at the issuer for replacement tokens within the existing grant's constraints; detailed in Identity token lifecycle spec. | Opaque secret; wire syntax outside this contract. |
| PAT | A product-verified personal access token credential kind with fixed resource, tenant, scope and lifetime constraints; detailed in Arc PAT spec. | Per-resource prefix and checksum; detailed in Arc PAT spec. |

## 1. Scope and status

This contract defines the person-facing identity shared by Identity, AuthProxy, Arc, Direct and Studio. Keywords follow [the specification conventions](README.md). It proposes the D4 person-id decision and related wire shapes; it does not claim deployment, owner review or sign-off. Implementation is [Identity #36](https://github.com/Cratis/Identity/issues/36).

**Open: needs Direct/Studio owner review.** Both owners MUST review the account, resource, tenant, claim and verified-caller mappings before acceptance in #14. Every wire mapping below remains Draft; rows marked **Review** specifically require that review. Realm topology (D1) chooses issuer values and security boundaries; **D1 does not change the `(issuer, subject)` shape**. The recommendation of a shared products realm and separate customer-app realms is not a resolved D1 decision.

## 2. Person, account and identifiers

| Term | Contract |
|---|---|
| Realm | An issuer trust boundary with its own accounts and signing keys. Sharing a process MUST NOT collapse realm identities. |
| Account | Identity's realm-local record: immutable subject, lifecycle state, linked upstream keys and separately protected profile. Identity is its sole writer. |
| Person key | The exact pair `(iss, sub)` issued by the realm. A product's person record references this pair; `sub` alone MUST NOT identify a person across realms. |
| Product person | The product-owned record resolved from a person key, with confirmed legacy keys during migration. Products own uniqueness and membership in their own records. |

### 2.1 Subject

Identity MUST generate a cryptographically random UUID version 4, encoded as 36 lowercase ASCII characters with hyphens:

```text
^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$
```

Example: `8b167ad2-0f89-4c78-9d42-6d4580495d32`.

This format is opaque, independent of providers and profile attributes, and compatible with the UUID allocation used today. A subject MUST NOT contain or be derived from email, upstream subject, login name, tenant or organisation. Consumers MUST compare it exactly and MUST reject malformed subjects, not lowercase, trim or repair them.

The subject MUST remain stable through renames, email changes and additional links. After account deletion, Identity MUST retain a durable, non-PII tombstone reserving `(iss, sub)` forever. Deletion, restoration, re-registration and backup recovery MUST NOT permit reuse, including reuse by the same human. A new registration receives a new subject; it does not inherit membership or grants. A deleted account MUST NOT authenticate. Existing valid UUID-v4 subjects MUST be preserved, not regenerated merely to migrate storage.

### 2.2 Issuers and upstream keys

A realm issuer MUST be a configured, absolute HTTPS URL with a lowercase DNS host, no user information, query or fragment, no explicit default port, and no trailing slash. An optional path MUST have nonempty segments and no dot segments. Configuration MUST publish one spelling; tokens and discovery MUST use that exact value. Verifiers MUST match an allowlisted issuer exactly, without URL normalization or following aliases. Issuer changes create a different person-key namespace and require an explicit migration; a signing-key rotation does not change the issuer. URLs here such as `https://identity.example` are examples, not the D1 selection.

Each linked upstream key is the exact pair `(idp issuer, idp_sub)`:

- `idp` is the upstream authority identifier registered on a trusted connection, not a display name such as `github`. For OIDC it MUST equal the validated upstream `iss`, including its exact spelling (which may differ from the realm-issuer URL convention). For GitHub OAuth, the adapter MUST use its configured authority, e.g. `https://github.com`, and the authenticated numeric user id rendered as a positive decimal string. For SAML, it MUST use the validated entityID and a connection-approved, persistent, non-reassigned subject, never a transient NameID or email.
- `idp_sub` MUST be a nonempty, immutable, non-reassigned upstream identifier, compared exactly as a string. Providers without such an identifier MUST NOT be used for automatic linking or migration. Upstream subjects need not match the realm UUID syntax.
- `auth_time` records the time of the actual authentication proving this key, not the time of a profile update or refresh. Per-connection assurance mapping supplies `acr` and `amr`; unknown or unmapped evidence MUST yield `basic`, never elevation.
- Within a realm, one upstream key MUST belong to at most one account, enforced atomically by Identity's authoritative writer, including concurrent attempts. Different issuers with equal `idp_sub` values are different keys; they MUST NOT imply the same person. The same upstream key in another realm conveys no cross-realm account equivalence.
- Email, verified email, display name and login name are mutable attributes, never keys or evidence of account equivalence. Confirmed links are immutable facts; corrections MUST be new audited facts, not edits to historical identity.

## 3. Linking, migration and quarantine

An explicit link MUST record authenticated intent and fresh proof of control of both the existing account and the candidate upstream identity in the same session-bound linking flow. Proofs MUST be bound to that flow, realm and target account, and protected against replay and cross-session substitution. With no conflict, the link MUST be confirmed atomically after uniqueness and tombstone checks, and audited, subject only to an optional realm-configured confirmation hold (§3.1); there is no mandatory hold. A profile email match MUST be refused as a linking method even when both emails are verified.

When a person signs in through Identity, migration MAY automatically confirm a link only when the immutable upstream key matches exactly one trusted legacy record with no conflict: the GitHub numeric id carried by `github_id`/`idp_sub`, only when `idp` is the registered GitHub issuer for that sign-in, or AuthProxy's canonical `(provider key, authority, subject)` mapped through the registered connection. That sign-in supplies the upstream proof and the trusted legacy record supplies the legacy-side evidence; a separate in-session proof of the legacy account is not required. Confirmation MUST be audited and enforce uniqueness and tombstones atomically, subject only to an optional realm-configured confirmation hold (§3.1). A legacy login name, unverified forwarded header or email never qualifies as migration evidence. Account-to-person uniqueness MUST also be enforced by each product's single writer; a confirmed key MUST NOT silently move between product people.

### 3.1 Link proposal state machine

This state machine concerns adding a link to an existing account or migrating a legacy person, not first registration of a new account from an unclaimed, verified upstream key. First registration MUST still enforce uniqueness and tombstones atomically.

| State | Entry and permitted transitions | Authority |
|---|---|---|
| `pending_proof` | Explicit intent recorded or migration sign-in under evaluation; no link or authority granted. Valid explicit-link proofs or exact-match migration evidence under §3, with no conflict and any configured hold satisfied, move it atomically to `confirmed`. Ambiguous, conflicting or untrusted legacy evidence moves it to `quarantined`; cancellation or invalid/expired control proof moves it to `rejected`. | Identity linking flow; requester may cancel. |
| `quarantined` | An ambiguous, conflicting or untrusted-evidence proposal is isolated and grants nothing. Only an authorized resolver's recorded clearance with valid evidence under §3 and atomic uniqueness and tombstone checks permits `confirmed`; resolver refusal moves it to `rejected`. Time alone MUST NOT confirm. | Authorized realm or product identity resolver, within its own authority. |
| `confirmed` | The link or mapping is committed atomically after uniqueness and tombstone checks, and audited. | Identity commits the account link; the product's single writer commits its person mapping. |
| `rejected` | Terminal for cancellation, invalid/expired control proof or resolver refusal. A new attempt MUST use a new flow; it MUST NOT reuse proofs. | Identity, or the authorized resolver for quarantined proposals. |

Each realm MUST configure proof expiry and MAY configure a confirmation hold; the default is no hold, and the hold policy remains open for owner approval (see open points). A non-conflicting proposal awaiting a configured hold remains `pending_proof`, not `quarantined`, and grants nothing. The hold starts when complete required evidence is recorded. If evidence expires during the hold, fresh proof is required; time alone or lack of proof MUST NOT cause confirmation. Quarantine is reserved for ambiguous, conflicting or untrusted-evidence cases, not routine linking or exact-match migration.

A collision with an already confirmed key MUST refuse the proposed binding and place the conflicting proposal in `quarantined`. The existing link MUST remain unchanged; no automatic account merge or key transfer is permitted. Concurrent claims MUST have at most one winner; any loser enters conflict quarantine. Ambiguous legacy matches and untrusted legacy evidence MUST also be quarantined and grant nothing through the proposed mapping.

Only a designated realm identity administrator may resolve an Identity link conflict; only the product's designated identity administrator may resolve a product-person conflict. Both MUST record evidence, a reason and any clearance. An organisation administrator alone MUST NOT override a realm-wide key constraint. A resolver may reject a proposal or clear an ambiguity supported by immutable evidence, but MUST NOT waive the applicable evidence requirements in §3, any configured hold, tombstones or uniqueness. Untrusted legacy evidence MUST be replaced by trusted evidence before clearance. Replacing an upstream issuer MUST NOT relink equal subject strings automatically: it requires a documented, verified migration for the same directory authority, fresh organisation-admin assurance, and the linking checks above. Email equality is never migration evidence.

## 4. Resource, tenant and organisation

| Identifier | Format and rules |
|---|---|
| Resource indicator | A registered absolute HTTPS URL using the realm-issuer URL conventions in §2.2, e.g. `https://direct.cratis.io`. Compared exactly; not inferred from an untrusted request host. Sent as OAuth `resource` on authorization and token requests. |
| Audience | One catalog-assigned ASCII string, 1–64 characters, matching `^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$`, e.g. `direct-api`. A catalog maps one resource indicator to one audience and scope prefix. Tokens MUST contain exactly one audience string, not an array or a list of products. |
| Scope | Catalog-assigned `<prefix>:<operation>`, e.g. `direct:read`, `direct:write`, `direct:work`. Prefix and operation each match `^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$`. There MUST NOT be an approve or admin scope. Scopes are ceilings, not roles or membership. |
| Tenant id | Product-assigned opaque ASCII string of 1–64 characters matching `^[a-z0-9](?:[a-z0-9-]{0,62}[a-z0-9])?$`, e.g. `t-1234`. Stable and never reassigned within the product installation. MUST NOT encode PII. Its identity is scoped by resource and installation, not globally inferred from the string. |

**Review:** these formats, catalog values and mappings need Direct/Studio owner review. A product whose existing tenant ids fall outside this syntax MUST map each such id to a stable alias that matches the syntax and record that mapping; it MUST NOT normalize ids silently (for example, by trimming a trailing hyphen). Mappings MUST be one-to-one: one existing id maps to one alias, and one alias maps to one existing id. Before use, an alias MUST be checked for collision with every existing valid tenant id and alias in that product installation; a collision MUST be refused. Aliases MUST remain permanently reserved after deletion and MUST never be reassigned.

An **organisation** is Access's commercial/administrative unit for cross-product policy and confirmed tenant bindings, not workspace membership. A **workspace** is a product **tenant**; its product owns membership and roles. A Chronicle **namespace** is storage, resolved through a registered installation and tenant binding, never directly from a caller-controlled string. Product UI may currently call a workspace an organisation; that label does not conflate the identifiers. Whether organisations are global remains D5.

Identity and AuthProxy MUST obtain selectable tenants from the product's authenticated account-key callback. A selection or cached answer is not authorization: the product MUST recheck live membership on every request. Configuration-only directories are development fixtures.

Tenant, resource and environment are immutable credential constraints. AuthProxy MUST independently resolve the requested target and refuse mismatches before any downstream call or namespace selection, even if the person belongs to both tenants. Environment MUST be bound in the trusted catalog/installation and credential record; a separately encoded environment claim is not specified here. A session without a selected tenant may reach only login, tenant listing and selection. Switching tenants MUST rerun selection and policy checks, not rewrite an existing token's tenant.

## 5. Token claims

This table defines person OAuth access tokens, not ID tokens, refresh-token internals, PAT secrets or workload tokens. Access tokens MUST be signed JWTs with `typ=at+jwt` and follow the RFC 9068 profile. Verifiers MUST check trusted issuer, signature/algorithm, expiry, token type, resource audience and required claims; an ID token MUST NOT be accepted as an access token. Refresh MUST NOT invent a new authentication time or increase authority.

`R` means required; `C` means required under the stated condition; `O` means optional. NumericDate values are integer UTC seconds since the Unix epoch. **Review** means **Open: needs Direct/Studio owner review**, not an accepted wire mapping.

| Claim | JSON type | Presence | Source and meaning | Review |
|---|---|---|---|---|
| `iss` | string | R | Configured realm issuer (§2.2); part of the person key. | Review |
| `sub` | string | R | Account UUID (§2.1), always the person, never an agent or upstream login. | Review |
| `aud` | string | R | Resource catalog audience; exactly one resource (§4). | Review |
| `exp` | integer | R | Issuer's short-lived expiry, constrained by grant/root expiry. Exact lifetime remains open. | Review |
| `iat` | integer | R | Issuer's token issuance time; MUST precede `exp`. | Review |
| `jti` | string | R | Issuer-generated opaque, unique token identifier, not a secret. | Review |
| `client_id` | string | R | Registered OAuth client identifier; no assertion that a public client is authenticated. | Review |
| `scope` | string | R | Space-delimited, catalog-valid scope ceiling from consent/grant and current policy. | Review |
| `tid` | string | C: tenant-bound resource | Selected product tenant id (§4). This is the **tenant** claim; `tenant_id` is not an additional alias. Person tokens authorizing workspace operations MUST carry it. | Review: name and legacy mapping |
| `org` | string | O: organisation relevant | Access's opaque organisation id from a confirmed binding, never inferred from `tid`; not membership. Identifier format remains open; MUST NOT be used for authorization until agreed. | Review |
| `idp` | string | R for upstream-authenticated person | Trusted connection's exact upstream issuer/authority used for this authentication, not every linked provider. | Review |
| `idp_sub` | string | C: upstream authentication | Immutable upstream subject verified by that connection. Listed privacy exception for migration/assurance; not the realm `sub`. | Review: exposure/minimization |
| `auth_time` | integer | R for person authentication | Actual verified person-authentication time; refresh/exchange MUST preserve it. | Review |
| `acr` | string | R for person authentication | Connection-mapped assurance level; unknown evidence is `basic`. Level vocabulary beyond `basic` remains open. | Review |
| `amr` | array of strings | O | Verified, connection-mapped methods; omit if unknown, never copy untrusted methods as elevated evidence. | Review |
| `act` | object | C: delegated/on-behalf | Current actor's registered `iss` and `sub` strings; optional nested `act` records earlier actors. Top-level `sub` remains the person. Only current actor and top-level claims authorize; prior actors are informational (RFC 8693). Actor namespaces/registration need review. | Review |
| `cnf` | object with `jkt` string | C: DPoP-bound | Issuer-verified DPoP public-key thumbprint (RFC 9449); a bound token MUST NOT be accepted without a valid matching proof or as plain bearer. | Review |
| `github_id` | string | C: GitHub migration consumer | Positive decimal immutable GitHub id, equal to `idp_sub` when this authentication is GitHub; emitted only to registered migration consumers. Removal requires coordinated migration. | Review: retirement and audience |

Roles, membership, email, display name, login name and arbitrary upstream claims MUST NOT appear in these access tokens. The only exception is `preferred_username` below, and only in an access token for a resource whose catalog entry opts in. Additional claims require contract review/versioning, not pass-through. Browser ID-token claims and user-facing profile retrieval need a separate contract; apart from `preferred_username` below, this contract does not authorize PII there.

### 5.1 Display claim `preferred_username`

`preferred_username` (string, optional) is the only display claim. Its value is the login from the latest `UpstreamKeyDisplayIdentifierObserved` for the account's **primary link**, which is the linked key of the connection used for this authentication (`idp`). For a refresh, it is the link recorded at authentication, so the value never mixes providers. It MUST be omitted when no display identifier is recorded.

- It MAY appear in ID tokens and userinfo responses.
- It MUST NOT appear in access tokens unless the resource's catalog entry explicitly opts in. Even then it is display data only and is not part of the authorization context.
- It is mutable, not unique and not verified as an identifier. It MUST NEVER be used as an authorization, matching, linking, migration or account-resolution key; `sub` (the account id) is the key. Consumers MUST NOT persist it as an identifier or join on it.
- It is personal data. It follows the Account erasure boundary in [identity-events-v1 §5](identity-events-v1.md), and caches holding it MUST honour erasure.
- Requesting it needs the OIDC `profile` scope, or the catalog opt-in for access tokens. It is never inferred from an unverified forwarded header.

## 6. Verified caller

Arc MUST build exactly one verified caller for UI, REST and MCP from a verified AuthProxy envelope, a locally verified PAT after proxy dispatch, natively validated OAuth where enabled, or an allowlisted verified workload credential. Unsigned identity headers MUST NOT authenticate a caller. PATs resolve the account key and constraints recorded at creation; they are not JWTs and do not inherit JWT claim parsing.

All field mappings below are **Open: needs Direct/Studio owner review**. These are logical fields, not a new AuthProxy envelope serialization. Envelope versioning and cryptographic framing remain the separate AuthProxy/Arc contract.

| Field | Logical type | Presence | Trusted source |
|---|---|---|---|
| Account key | `{ issuer: string, subject: string }` | R for person; absent for service | Verified `iss`/`sub`, or verified PAT/session record. |
| Person | Product person reference | R for person; absent for service | Product's confirmed key mapping; unresolved/quarantined proposals confer nothing. |
| Resource / environment | Registered resource and environment references | R for resource access | Verified audience/credential constraints plus trusted installation binding. |
| Tenant | string | R for workspace access | Independently resolved signed target equal to credential tenant, binding and live membership. |
| Organisation | Opaque reference | O | Access's confirmed tenant binding; not tenant membership. |
| Credential kind / id | Enum / non-secret string | R | Verified session, OAuth grant, PAT, on-behalf session, machine principal or workload record. A token `jti` alone is not a grant id. |
| Scopes | Set of strings | R | Verified credential ceiling, intersected with live product roles and policy. |
| Client | `{ id: string, authentication: declared or authenticated }` | C: client present | Verified client registration/authentication; public client ids are declared. |
| Actor / grant | Actor reference/chain and bounded-grant reference | C: acting for another | Verified `act` and authoritative grant/support records; nested prior actors do not authorize. |
| Assurance | `idp`, `auth_time`, `acr`, optional `amr` | R for person | Mapped authentication evidence or verified credential-creation evidence for PATs; evaluated against current policy. |
| Sender binding | None or verified DPoP result (`jkt`, accepted proof `jti`) | R | Proxy/native verifier checks key, target, method, time, token hash and replay protection; MUST NOT forward a raw proof as verified evidence. |
| Policy epoch | Nonnegative integer | C: organisation policy applies | Authoritative epoch and policy projection at or after it; stale credentials require re-evaluation, not trust in an old claim. |
| Contract version | string | R | Signed context version actually verified (or the local verifier's explicitly supported profile); never caller input. |

A workload produces a service caller with no person; this contract MUST NOT turn a workload subject into a person UUID. Any person's authority in workload operations comes only from a separately verified per-work grant intersected with service permissions. Delegation MUST NOT widen tenant, resource, environment, assurance, scopes or lifetime; grant lineage is server-side, and ancestor revocation revokes descendants. Token evidence never replaces live membership or current policy checks.

## 7. Privacy and persistence

The subject is pseudonymous, not anonymous: products can correlate it within the realm. PII MUST live only in protected account-profile event payloads and their derived access-controlled projections, with personal fields marked for PII handling from their first event generation. Identifiers MUST NOT embed PII. Immutable upstream identifiers are link evidence, potentially personal data: their raw values MUST be protected in those account-profile payloads, never used as event-source ids. Public link events MUST use pseudonymous link references rather than raw upstream identifiers. Profile PII MUST NOT be copied into grant, membership or audit events.

No secret, upstream token, credential, authorization code or raw proof may be recorded in an event or vector. Access-token personal evidence is limited to the claims explicitly listed in §5, including the reviewed upstream-id/migration exceptions. Implementations MUST minimize their audience and exposure, and MUST NOT add email or login for convenience. Tombstones retain only the minimum pseudonymous reservation necessary to prevent reuse, not erased profiles. Account erasure and link-evidence retention require a reviewed privacy workflow; deleting a profile MUST NOT release its subject.

## 8. Current state and migration

This section describes inspected code, not additional normative claims about deployed systems:

- `Source/Identity/Accounts/Account.cs` and `AccountDocument.cs` hold a string account id, numeric GitHub id, login and name. `Accounts.cs` allocates `Guid.NewGuid().ToString()` on insertion and matches sign-in by numeric GitHub id. The generalized realm/upstream-key model, tombstones and quarantine remain implementation work.
- `Source/Identity/Upstream/GitHubUserResponse.cs` reads the positive numeric GitHub id separately from login. `GitHubSignInEvents.cs` replaces the upstream principal with the account session principal. `SessionPrincipal.cs` currently adds `sub`, name, GitHub id and GitHub login to the site session; that is not the target access-token allowlist.
- `Source/Identity/Tenants/SelectableTenant.cs` documents `tid`. `ConfiguredTenantDirectory.cs` selects configured resource tenants using GitHub-id membership; `ITenantDirectory.cs` already exposes the product-owned directory seam. This Draft retains `tid` as the proposed wire name, pending consumer review.
- Direct currently consumes `sub`/`ClaimTypes.NameIdentifier`, GitHub/login-oriented identity claims and `x-ms-client-principal*` headers (e.g. `DirectIdentityDetailsProvider.cs`, `PersonalAccessTokens.cs`). Studio similarly consumes `sub`/`ClaimTypes.NameIdentifier` and forwarded principal ids (`TenantSelection.cs`, `OrganizationContext.cs`). Those compatibility paths do not establish agreement on the target `(iss, sub)` or verified-caller mappings.

Migration MUST preserve valid account ids, add issuer scope and generic immutable links, and resolve product legacy keys only through §3. Consumers MUST NOT reinterpret an existing login name as a canonical subject. Existing incompatible identifiers require explicit audited mappings and conflict quarantine before cutover. Public-key/issuer choices, tenant aliases and legacy-claim retirement require owner review; this document changes no runtime behavior.

## 9. Machine-readable vectors

[Data vectors](../../Source/Identity.Specs/Vectors/identity-contract-v1/) contain one JSON object per file with `name`, `description`, `given`, `when` and `then`. Inputs describe trusted stored state and verified authentication evidence unless explicitly stated otherwise. `when.action` names a logical operation, not an API endpoint. `then` gives required assertions; outcomes and reason strings are vector vocabulary, not public error messages. `profile` contains mutable attributes and MUST NOT be interpreted as identity evidence. Example hosts and ids are synthetic.

These are **data only**. They add no C# tests or project configuration and do not demonstrate runtime conformance. Implementation #36 MUST consume them in executable specs, including atomic uniqueness and tombstone enforcement. Token and proxy conformance need additional executable coverage in their respective implementations.

## 10. Open points and required sign-off

- **D1:** select realm topology and exact issuer URLs; it does not change the `(issuer, subject)` shape. D13 separately needs explicit acceptance of any shared issuer-key risk.
- **Direct/Studio review:** both owners must review account resolution, resource/audience catalog, tenant format/`tid` mapping and every Review claim/caller mapping before acceptance; that review has not happened here.
- **D5:** confirm organisation scope, ownership and organisation-id syntax; organisation is not a product tenant or membership authority.
- **Link lifecycle:** approve optional realm confirmation-hold policy (default none), proof freshness/expiry policy, resolver authorization and recorded-clearance requirements, and documented issuer-migration procedure; no time-based automatic approval.
- **Token profile:** confirm RFC 9068/OpenIddict conformance, lifetime/skew policy, actor namespaces and assurance/method vocabulary with Identity, AuthProxy and Arc owners.
- **Privacy/migration:** approve upstream-id exposure and retention, protected link evidence, GitHub migration audiences/retirement and erasure/tombstone handling.
- **D20 / envelope:** confirm policy-epoch freshness and signed-context field mappings in their own versioned contracts; define any explicit environment wire claim there if needed.
- **Lock:** obtain the required consumer reviews and sign-off in Identity #14. Until then this specification and its vectors remain Draft.

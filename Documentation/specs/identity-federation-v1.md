# Federation contract and issuer-scoped relink

```text
Spec id: identity-federation
Version: 1
Status: Draft
Issue: https://github.com/Cratis/Identity/issues/30
Sign-off issue: https://github.com/Cratis/Identity/issues/14
Supersedes: None
```

## Terms

The [Terms of identity-contract-v1](identity-contract-v1.md#terms) apply verbatim. This document adds:

| Term | Meaning |
|---|---|
| Connection | An Access-approved upstream authority and protocol configuration, identified by the event catalogue's opaque ConnectionId. It is not an account or a linked key. |
| Connection replacement | Changing the approved configuration used for authentication; it does not transfer accounts or rewrite confirmed links. |
| Issuer migration | A documented, verified procedure associating explicitly mapped old and new issuer-scoped keys with an existing account, under §4. |
| Assurance mapping | A revisioned, closed typed per-connection mapping of validated upstream authentication evidence to Identity assurance. |

## 1. Scope and status

This draft specifies OIDC/SAML connection-to-key mapping, per-connection assurance, and issuer-scoped relink invariant M11 from [Identity #30](https://github.com/Cratis/Identity/issues/30). It claims no deployed behavior. The [contract](identity-contract-v1.md#22-issuers-and-upstream-keys), [event catalogue](identity-events-v1.md#22-stream-boundaries), [token profile](identity-access-token-v1.md) and [tenant model](identity-tenant-model-v1.md) remain authoritative; this draft does not amend signed-off files.

D1 = B ([#21](https://github.com/Cratis/Identity/issues/21)): Direct and Studio share the products realm; customer Stage applications and self-hosted installations have separate realms. D5 = A ([#23](https://github.com/Cratis/Identity/issues/23)): Access owns organisation identity, verified domains, connections, policies and confirmed tenant bindings. Products remain the only writers of membership, roles and business authorization. D13 ([#15](https://github.com/Cratis/Identity/issues/15)) accepts the shared signing authority only with the production-issuance gates in [contract §10](identity-contract-v1.md#10-decisions-recorded-at-sign-off); federation does not discharge those gates.

Organisation administration MUST NOT create tenant membership. Authentication through a connection MUST NOT grant product access without live product membership, roles, scopes, current policy and verified credential constraints. An upstream token MUST NOT be accepted directly as a Cratis resource token through this specification.

## 2. Connection to linked key

Access MUST approve a connection's authority and protocol before it can supply authentication evidence. A connection MUST supply evidence for an issuer only when both conditions hold:

- Exactly one approved connection lineage in the realm holds that issuer, or Access has verified that a replacement belongs to the same organisation and directory authority as the existing lineage. A connection from another organisation MUST NOT claim an existing lineage's issuer.
- Verification keys and metadata come from the issuer itself (OIDC discovery or JWKS at the exact `iss`), or, for SAML, the signing trust anchor is verified to belong to that exact entityID. A connection administrator's choice of metadata, keys or entityID alone MUST NOT establish this binding. Verified key rotation is allowed; identical certificates are not required when trust-anchor continuity has been verified.

Conflicting registrations MUST be refused or quarantined and MUST NOT supply authentication evidence, resolve an account or grant access. Identity MUST validate the authentication through the registered connection and its verified issuer/trust-anchor binding before extracting a key; an unverified issuer, assertion, forwarded header or caller-supplied provider label MUST NOT select an account. The event-catalogue extension for recording lineage and continuity evidence is outstanding in §7.

| Protocol | `idp` | `idp_sub` |
|---|---|---|
| OIDC | Exact validated upstream `iss`, equal to the registered authority | Validated nonempty immutable, non-reassigned `sub` |
| SAML | Exact validated entityID of the approved connection | Connection-approved persistent, non-reassigned subject; never a transient NameID or email |
| GitHub OAuth | Configured registered authority, for example `https://github.com` | Authenticated immutable numeric user id rendered as a positive decimal string |

A linked key is the exact string pair `(idp, idp_sub)`, scoped to the realm. Consumers MUST NOT normalize, trim, lowercase or alias either value. Upstream issuer spelling need not follow the realm-issuer URL convention. Identical subjects from different issuers are different keys. Email, verified email, login, display name and connection labels MUST NOT establish equivalence. Providers without a stable, non-reassigned subject MUST NOT support automatic linking or migration.

Each successful authentication supplies its actual `auth_time`, `idp`, `idp_sub`, and mapped assurance. `auth_time` MUST describe the authentication that proved the key, never connection registration, profile update, token refresh or migration approval time. Refresh/exchange MUST preserve it and MUST NOT elevate assurance ([token profile §4](identity-access-token-v1.md#4-lifetimes-and-refresh)).

Lookup MUST use a keyed digest of the exact upstream pair outside event-source ids. Raw `idp_sub` or a raw pair MUST NOT enter stream ids, public link events or general logs. Protected Internal account-profile link evidence follows [contract §7](identity-contract-v1.md#7-privacy-and-persistence) and [events §5](identity-events-v1.md#5-pii-and-erasure). Public link facts use pseudonymous references; tokens, SAML assertions, raw proofs and secrets MUST NOT enter events, audit records or vectors. The catalogue's Connection stream id is an opaque reference, not an issuer or subject.

## 3. Per-connection assurance

Access MUST own a closed typed table of approved source/target `acr`/`amr` mappings for each connection, with a mapping revision. Authentication MUST use only validated evidence from that connection and its applicable mapping revision. Arbitrary upstream claims, self-asserted method labels and another connection's mapping MUST NOT elevate assurance. Recording a separately approved maximum assurance requires the event-catalogue extension in §7; no existing Access fact records that maximum. Until that extension exists, a result MUST be bounded by the approved mapping table, or `basic` when no valid mapping applies. Once recorded, the connection's approved maximum MUST additionally bound every result.

Unknown, absent or unmapped evidence MUST yield `acr=basic`, never elevated assurance. Unknown methods MUST NOT be invented or copied into Identity `amr`; omit them or use a verified empty collection as allowed by the caller contract. Mapped method evidence MUST remain distinguishable from an upstream label. Level vocabulary beyond `basic` remains open in the token profile; this draft defines no new level names.

`EnterpriseConnectionRegistered` records the approved authority and protocol. `ConnectionAssuranceMappingChanged` records the closed typed mapping revision; neither contains a secret or arbitrary claims object. Mapping changes apply only to authentications after the change. They MUST NOT reinterpret historical authentication, rewrite an old `auth_time`, or promote existing sessions or refresh credentials. A new authentication is required to obtain new mapped evidence. Existing evidence still faces current policy, freshness, revocation and recovery checks; immutable history is not permission to retain privileges after tightening.

## 4. Issuer-scoped relink invariant M11

### 4.1 Same issuer

Replacing a connection with another approved connection for the **same exact issuer** MAY resolve an already confirmed exact key to its existing account only after normal verified authentication and satisfaction of §2's issuer-lineage, same-organisation/directory-authority replacement and trust-anchor binding requirements. Equal issuer strings alone MUST NOT establish continuity. No new link proposal is needed merely because ConnectionId changes: the key has not changed. This MUST NOT transfer the key to a different account, skip atomic uniqueness or revive an unlinked/reserved key. The new connection's assurance mapping applies to the new authentication; connection continuity is not assurance continuity.

### 4.2 Different issuer

A new connection with a **different issuer and the same subject MUST NOT automatically relink** the old account. Matching email or organisation does not change this result. A proposal to add the new key to an existing account MUST follow [contract §3](identity-contract-v1.md#3-linking-migration-and-quarantine): authenticated intent, fresh proof of control of both sides in the same realm/account/session-bound flow, replay protection, atomic uniqueness/tombstone checks and any configured confirmation hold. A pending or quarantined proposal grants nothing. First registration from a new unclaimed key is separate and MUST NOT inherit the old account or its grants.

### 4.3 Verified issuer migration

An issuer change is permitted only through a documented, verified migration for the **same directory authority**, approved by an authorized organisation administrator with fresh assurance. The continuity of that directory authority MUST be verified; sharing an organisation label is insufficient. The procedure MUST identify the realm, organisation, old/new approved connections and exact old-to-new key mappings; record verified evidence and approval; and bind approval to that procedure so it cannot be replayed for another mapping or session. Name/email similarity alone MUST NOT count as verification.

Approval MUST use reauthentication within the realm-configured proof freshness/expiry window and satisfy current organisation assurance policy. No numeric migration window is defined here; the explicit configured window and its measurement remain sign-off work (§7). Missing, expired, unmapped or inadequate evidence MUST deny approval. An organisation administrator role is Access authority for this approval, not product membership and not proof of control of an account.

Identity MUST still enforce contract §3's linking proofs, resolver clearance where required, atomic uniqueness and tombstones. Admin approval alone MUST NOT replace these requirements. Conflicting keys MUST remain quarantined, with existing links unchanged. Every accepted migration MUST be audited without raw proof, credential or personal identifier content in general audit records. The realm account subject MUST NOT change, and facts MUST be appended rather than rewriting historical links.

This draft does not authorize a bulk proof waiver or reinterpret the contract's exact-match legacy migration exception as cross-issuer equivalence. Any such procedure would require a separate versioned contract amendment and sign-off before use. Migration approval/audit persistence and any new event contracts are outstanding in §7; no event id or new wire format is assigned here.

### 4.4 Unlink and connection isolation

`UpstreamKeyLinked` and `UpstreamKeyUnlinked` retain their catalogue meanings. An unlinked key stays reserved to its old account. It MUST NOT be automatically relinked even to that account; the catalogue requires an explicit reviewed operation and its own extension. Replacement or migration MUST NOT bypass that reservation or any account tombstone.

Removing one connection MUST NOT unlink keys authenticated through another connection or revoke its otherwise valid independent access. Removing/unlinking affected authority still enforces the [revocation target rules](identity-revocation-v1.md#7-revocation-targets-and-verifier-bounds); connection isolation MUST NOT rescue a credential fenced through its own authentication lineage. Products still decide live membership on every use. Federation across realms MUST NOT infer account equivalence from an equal upstream pair.

## 5. Non-goals

This draft does not implement SAML ([#67](https://github.com/Cratis/Identity/issues/67)), runtime federation enforcement, direct external-token verification, SCIM, a universal membership directory, binding UX or a new assurance vocabulary. It defines no connection-removal event schema or bulk account merge. The M8 portability proof remains implementation work: a Cratis-hosted application and customer installation each federate an enterprise IdP for two tenants, refuse cross-tenant credential reuse, preserve the other connection's access on removal, support Ante bootstrap and retain grants across upgrades. These data vectors do not prove that gate.

## 6. Machine-readable vectors

[Data vectors](../../Source/Identity.Specs/Vectors/identity-federation-v1/) contain one JSON object per file with `name`, `description`, `given`, `when` and `then`. `given` describes trusted stored state and verified evidence unless marked otherwise. `when.action` is a logical operation, not an endpoint. Outcomes and reason strings are fixture vocabulary, not public error messages. Account/connection labels and `.example.test` hosts are synthetic; they are not identifier syntax. No real credentials or raw proofs are included. Migration booleans denote required verified evidence, not a production substitute for verification.

These are **data only**. They add no C# tests or project configuration and demonstrate no runtime conformance. A harness must exercise authentication validation, connection replacement, per-connection assurance, stale approval refusal, session-bound proofs, concurrent uniqueness, reserved keys and isolated connection removal against real authority boundaries. A valid link still creates no product membership or unconstrained access.

## 7. Outstanding decisions and amendments needed

- Obtain #14 sign-off. Confirm the documented migration procedure, its realm-configured freshness window and evidence-verification/audit responsibilities; this draft does not claim they are deployed.
- Specify a versioned event-catalogue extension for issuer-migration approval and audit evidence references, including privacy, authority, idempotency and lifecycle. Existing `EnterpriseConnectionRegistered`, `ConnectionAssuranceMappingChanged`, `UpstreamKeyLinked` and `UpstreamKeyUnlinked` MUST NOT be repurposed as approval or a proof waiver. No new event ids are invented here.
- Specify a versioned Access event-catalogue extension recording issuer reservations/approved connection lineages and verified same-organisation, directory-authority and issuer trust-anchor continuity, including verified key rotation. Use non-secret evidence references, not raw proofs or certificate blobs. Existing connection registration alone MUST NOT be treated as continuity verification; conflicting registrations remain refused or quarantined under §2. No new event ids are assigned here.
- Specify an Access event-catalogue extension or amendment recording each connection's approved maximum assurance; neither `EnterpriseConnectionRegistered` nor `ConnectionAssuranceMappingChanged` currently carries it. Until that extension exists, results MUST be bounded by the approved mapping table, or `basic` when no valid mapping applies (§3).
- Define the explicit reviewed operation/catalogue extension for any reserved-key relink if needed; until then it MUST deny. No signed-off contract or event file is edited by this draft.
- Complete the assurance vocabulary beyond `basic` and connection maximum/mapping validation without pass-through or retrospective elevation. Runtime validation and the portability proof remain separate implementation/release work.

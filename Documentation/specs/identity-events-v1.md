# Identity event rules and catalogue

```text
Spec id: identity-events
Version: 1
Status: Locked
Locked: 2026-11-05, https://github.com/Cratis/Identity/issues/14
Issue: https://github.com/Cratis/Identity/issues/26
Sign-off issue: https://github.com/Cratis/Identity/issues/14
Supersedes: None
```

## 1. Scope and authority

Keywords follow [the specification conventions](README.md). The [Terms of identity-contract-v1](identity-contract-v1.md#terms) apply **verbatim**; this document does not redefine them. Its identifier, linking, quarantine and tombstone rules remain prerequisites. This specification defines the initial event contract, not deployed behavior. einari owns the event rules; the choices in §9 were decided at the Identity #14 sign-off, as recorded there.

Design A §§4.9–4.11 and D18, and Design B §§6.1 and 7.9, require past-tense facts, stable ids, generations, public/internal separation, first-release PII protection and no redeemable content. Chronicle mechanics below were checked against its .NET client source and event-modeling, event-type-migration and compliance skills. Identity-specific policy stricter than Chronicle is identified as Identity policy, not attributed to Chronicle.

## 2. Names, streams and type identity

### 2.1 Names and schemas

Events MUST describe one completed domain fact, using a self-describing PascalCase `<Entity><PastTenseVerb>` name in `Cratis.Identity.Contracts.<Area>`. A request accepted as a business fact (for example an erasure request) is distinct from a transport wake-up. Events MUST NOT be commands, generic CRUD snapshots, ticks, cache observations or reactor wake-ups. `AccountUpdated` is forbidden: it hides which fact changed, its preconditions, privacy implications and consumer meaning. Use `AccountEmailChanged` or `AccountDisabled` instead. `Created`, `Updated`, `Deleted`, `Data`, `Payload` and `Event` alone are not event names. Names MUST NOT be renamed, removed or reassigned after publication; a different meaning needs a different event. The detailed naming/area vocabulary is decided in §9 point 1.

Event properties MUST be non-nullable, typed domain values. An optional fact SHOULD be a separate event, not a nullable placeholder. The event's own event-source id MUST be in Chronicle context, not duplicated as a payload property. Referenced entities' ids MAY appear in payloads. Chronicle event id, sequence and timestamp are context, not alternate business identities. Time fields in §8 mean UTC instants; lists are non-null collections; reason fields are closed, non-PII reason identifiers, never free-text evidence. Fields are the initial generation's minimum schema (§9 point 3); additions before first append require review, and later changes follow §3.

### 2.2 Stream boundaries

Each realm MUST have its own Chronicle event store. Context and delivery MUST preserve the exact configured realm issuer so consumers resolve accounts as `(issuer, subject)`, never subject alone. Chronicle's event-source id is a string, but the following boundaries are the proposed Identity policy (open point 2):

| Stream alias in §8 | Event source id | Rules |
|---|---|---|
| Account | Account subject | Exactly the immutable lowercase UUID v4 in contract v1 §2.1. Link proposals and confirmed links remain on the target account stream. |
| Grant | GrantId | Opaque, generated, never-reassigned UUID v4; one authorization grant, not a token. |
| Family | FamilyId | Opaque, generated, never-reassigned UUID v4; one refresh-token family, not a refresh-token value. |
| Client | ClientRegistrationId | Opaque UUID v4, distinct from a client's externally registered OAuth identifier or CIMD URL. |
| Signing key | SigningKeyId | Opaque UUID v4 identifying a secret-store/KMS record; not key material. |
| Resource | ResourceId | Opaque UUID v4 referencing the contract v1 resource catalog entry. |
| Organisation | OrganisationId | Opaque Access id; syntax/ownership per D5 (Identity #23). |
| Binding | BindingId | Opaque UUID v4 for a registered `(resource, installation, tenant, environment, organisation)` binding; per D5 (Identity #23). |
| Connection | ConnectionId | Opaque UUID v4 for an approved upstream connection; ownership per D5 (Identity #23). |
| Realm | RealmStateId | One opaque UUID v4 allocated once per realm, resolved through its configured issuer, never a deployment hostname. |

Stream ids MUST NOT contain PII, email, name, login, `idp_sub`, a raw linked-key pair, secrets or hashes derived from personal identifiers. No realm/account/key/tenant identity may be inferred by normalizing or parsing an arbitrary stream string. Referenced tenant ids retain contract v1's resource/installation scope and syntax; bindings MUST NOT invent global tenant ids or membership authority. Link lookup MUST use a keyed digest of the exact upstream pair, outside the stream id; it MUST enforce the contract's atomic uniqueness and quarantine rules.

### 2.3 Stable event type ids

The real attribute is `Cratis.Chronicle.Events.EventTypeAttribute(string id = "", uint generation = 1)`, written `[EventType("<id>", generation: 1)]`. `Cratis.Chronicle.Events.EventTypeId` is a **string**, not a required GUID. Omitting the id defaults to the CLR type name; Identity MUST NOT rely on that default.

Decided (§9 point 1): an explicit lowercase UUID v4 string for each event kind, assigned once by the contracts implementer before registration/first append. Each §8 `assigned at implementation` cell MUST then be replaced with that id in the same change as its attributed type and vectors. The catalogue is the checked-in registry: ids MUST be unique across Identity event kinds and MUST never be reused, even after retirement. All generations of one event share that id. An event type id is neither an event instance id nor an event-source id. Moving CLR code MUST NOT change the pinned id or published meaning.

## 3. Generations and compatibility

An event's persisted schema MUST change only through a new, consecutive generation starting at 1, under the same stable type id. This deliberately stricter Identity policy includes additive fields, PII/subject metadata changes and enum additions even where Chronicle considers them compatible (§9 point 4). Documentation-only corrections are not schema changes. A new business meaning MUST use a new event, not a generation that reinterprets history.

Breaking changes include field addition/removal/rename, type or unit changes, requiredness/cardinality changes, nesting/splitting/merging, enum removal or renumbering, changed validation/meaning, or changed privacy/subject interpretation. A generation MUST NOT silently invent historical consent, proof, authority or personal data. If an honest transformation cannot be defined, preserve the old event and introduce a distinct fact.

The prior CLR shape MUST remain available. Chronicle's preferred marker is `[EventTypeGenerationFor<TCurrent>(n)]` (`Cratis.Chronicle.Events.EventTypeGenerationForAttribute<TEventType>`), not an unrelated type id. Implement adjacent `Cratis.Chronicle.Events.Migrations.EventTypeMigration<TUpgrade, TPrevious>` classes with both `Upcast` and `Downcast`, describing property and value mappings. Chronicle requires consecutive generations with the same id. Its API permits an empty downcast; Identity requires meaningful tested downcasts for every still-supported consumer, with documented loss of fields introduced later and no invented authority. Test each adjacent pair and the whole chain, including erased PII.

Old generations MUST remain readable forever, subject to intentional PII erasure/redaction: retain prior shapes, mappings, schema metadata and replay fixtures. This is not a promise that erased personal data can be recovered. Consumers MUST NOT assume all deliveries are automatically upcast to latest. Chronicle defaults to `Compatibility`; generation-pinned delivery is an explicit capability/configuration choice. W2 MUST prove the chosen package/kernel versions and observer delivery policy before publishing a generation (§9 point 4).

Chronicle's new-generation backfill is add-only in the default namespace event log; it is not a universal rewrite of other namespaces/sequences. Register only after all kernel silos are upgraded; inventory and test every used namespace/sequence and publication path. Never use a migration to repair a leaked secret or retroactively encrypt historical plaintext; treat that as a security incident and erasure/redaction workflow.

## 4. Public and internal contracts

A **Public** event is an intentionally supported inter-module contract. Products, Ante, AuthProxy, Arc policy projections and other approved consumers may react to its documented facts. It carries the permanence, stable-id, generation and erasure obligations above. It conveys facts, not live product membership or permission to bypass current policy/fences.

An **Internal** event is for Identity/Access's own projections, audit and workflows. External consumers MUST NOT depend on it. Its persisted history still obeys all schema, migration, privacy and secret rules; internal does not mean disposable or safe to leak.

Both sets in §8 are exhaustive for this initial catalogue (§9 point 3). Decided (§9 point 1): both are placed in `Cratis.Identity.Contracts.<Area>` so the check in §6 has no internal blind spot; a CLR-public type is not thereby a public integration event. Public events MUST carry Chronicle's `Cratis.Chronicle.Events.PublicAttribute` (`[Public]`). Internal events MUST NOT be effectively public: Chronicle can also infer public status from an `[EventStore]` naming the connected store, so absence of `[Public]` alone is insufficient. Export configuration MUST agree with the catalogue and MUST NOT publish internal events.

A Public event MUST NOT become Internal, disappear or change meaning. A replacement needs a **new event id and name**, continued support for the old contract and a reviewed consumer migration. This specification's version is separate from individual event generations. Consumers MUST be idempotent by event identity/sequence and MUST NOT mint credentials or repeat external side effects during replay.

### 4.1 Source facts and publication

Chronicle does not append a `[Public]` event type to the event log; Public contracts reach consumers only through the outbox. Every Public row in §8 therefore has exactly one **Internal source fact**, listed below, on the **same event source** as the Public contract:

1. **The writer appends only the source fact.** Atomic appends (for example `UpstreamKeySubjectRecorded` with the link's source fact), uniqueness constraints (the per-key `KeyHash` constraint on linking), concurrency scopes, projections, reactors inside Identity/Access and replay use the source fact, never the Public contract. Nothing appends a Public contract to the event log.
2. **A publication reactor publishes the contract.** One reactor per Public contract observes its source fact on the event log and appends the Public contract to the outbox. The Public payload is derived only from the source fact's fields and Chronicle context; it adds no other data, stays pseudonymous under §5 (no `IdpSub`, login or email even when an Internal fact on the same stream carries them) and keeps the source fact's event source, compliance subject, correlation and occurrence time.
3. **Identity and delivery.** Every source fact carries a `FactId` field: an opaque, non-PII, non-redeemable UUID chosen by the writer that survives retry, restore and re-append. For a fact dispatched from the revocation outbox it is derived from the entry's OutboxId (identity-revocation-v1 §3 step 5); otherwise it is derived from the accepted operation that produced the fact. The published event's identity is `<FactId>:<contract type id>`, used both as the outbox idempotency key and as the consumer deduplication tag, so a redelivery, replay or post-restore re-append of the source fact publishes nothing new. The Public payload does not carry `FactId`. Event-log sequence numbers MUST NOT form this identity: a restore can re-append a fact at a different sequence number or give a lost number to a different fact. Publication is at least once; consumers MUST deduplicate on that identity. A redelivered publication that finds its own earlier outbox append succeeds; any other failed append fails the reactor delivery and is retried, never skipped.
4. **Ordering guarantee.** For one event source, Public contracts are published in the event-log order of their source facts: a later source fact's contract is never published before an earlier one's, and a failed publication holds back later ones for that event source until it succeeds. A held-back publication MUST raise an operational alert naming the event source and the stuck source-fact sequence number, so a fact that cannot be published never stalls a stream silently. Consumers may rely on per-event-source order (for an account: registration before link, link before unlink, disable before enable). There is no ordering guarantee across event sources, and none between a Public contract and Internal facts.
5. **Latency.** Publication is asynchronous. A Public contract is never a prerequisite for enforcement: fences, epochs and tombstones (§7, identity-revocation-v1 §3) take effect before acknowledgement whether or not the contract has been published.
6. **Catalogue and generations.** A source fact is an Internal event under every rule of this specification (stable type id, generations, PII, forbidden content). It carries `FactId` and every field its Public contract publishes, with the same protection marks, so the contract never needs data from elsewhere. A new Public generation that needs a new field needs a new source-fact generation first.

Source facts are named `<Public contract subject><noun>Recorded` and listed here; their type ids are allocated under §2.3 like any other row. Each has the Public contract's area, event source and fields (with the same marks) plus `FactId`, is Internal and written by the contract's writer; §8 rows describe the contract.

| Public contract | Internal source fact | Source fact type id |
|---|---|---|
| AccountRegistered | AccountRegistrationRecorded | c2c8ac1b-6110-433f-93d6-1e9222852baa |
| AccountDisabled | AccountDisablementRecorded | e26026d0-d445-4647-ba9f-f7f1d868c8ac |
| AccountEnabled | AccountEnablementRecorded | 46c54931-10f5-4069-9c22-b059008b3fc1 |
| AccountTombstoned | AccountTombstoneRecorded | f67aa184-1052-4a3e-863a-13059de335e0 |
| AccountErased | AccountErasureRecorded | assigned at implementation |
| UpstreamKeyLinked | UpstreamKeyLinkRecorded | 0948769e-d249-472e-abb3-93e8743379da |
| UpstreamKeyUnlinked | UpstreamKeyUnlinkRecorded | de708442-c9b3-4d8b-b677-01450ef9248e |
| ConsentWithdrawn | ConsentWithdrawalRecorded | assigned at implementation |
| AuthorizationGranted | AuthorizationGrantRecorded | assigned at implementation |
| AuthorizationRevoked | AuthorizationRevocationRecorded | assigned at implementation |
| RefreshTokenFamilyRevoked | RefreshTokenFamilyRevocationRecorded | assigned at implementation |
| SigningKeyActivated | SigningKeyActivationRecorded | assigned at implementation |
| SigningKeyRetired | SigningKeyRetirementRecorded | assigned at implementation |
| ResourceRegistered | ResourceRegistrationRecorded | assigned at implementation |
| ResourceScopeRegistered | ResourceScopeRegistrationRecorded | assigned at implementation |
| ResourceScopeRetired | ResourceScopeRetirementRecorded | assigned at implementation |
| OrganisationRegistered | OrganisationRegistrationRecorded | assigned at implementation |
| OrganisationPolicyChanged | OrganisationPolicyChangeRecorded | assigned at implementation |
| OrganisationDomainVerified | OrganisationDomainVerificationRecorded | assigned at implementation |
| TenantBindingConfirmed | TenantBindingConfirmationRecorded | assigned at implementation |
| TenantBindingRemoved | TenantBindingRemovalRecorded | assigned at implementation |
| EnterpriseConnectionRegistered | EnterpriseConnectionRegistrationRecorded | assigned at implementation |
| ConnectionAssuranceMappingChanged | ConnectionAssuranceMappingChangeRecorded | assigned at implementation |
| RealmPolicyEpochAdvanced | RealmPolicyEpochAdvanceRecorded | assigned at implementation |
| RealmRecoveryEpochAdvanced | RealmRecoveryEpochAdvanceRecorded | assigned at implementation |

## 5. PII and erasure

Email, personal name/login and upstream subject (`idp_sub`) MUST be protected from the first production append, including generation 1. The real marker is `[PII]`, `Cratis.Chronicle.Compliance.GDPR.PIIAttribute`, on the concept type, property or matching constructor parameter (for positional records prefer `[property: PII]`). The rationale SHOULD use `Cratis.Chronicle.Compliance.ComplianceDetailsAttribute`; Chronicle's `PIIMetadataProvider` uses those details first and falls back to the `PIIAttribute` details argument only when no compliance details are present, so the two MUST NOT disagree. `[Encrypted]` is not a substitute for PII erasure and MUST NOT be combined with `[PII]` on the same value. `[PII]` does not make a secret permissible.

In §8, `†` means `[PII]` and `‡` means a keyed personal-data lookup digest, also `[PII]` (§9 point 5). Concepts MUST carry protection wherever reused. Only the minimum approved recipients may receive plaintext. Following contract v1 §7, **Public events MUST NOT carry raw upstream identifiers or upstream display identifiers**: public link events use the pseudonymous `LinkId` and `KeyHash` only. Raw `IdpSub` lives only in Internal Account-stream events: the pending `UpstreamKeyLinkProposed` and, for a confirmed link, `UpstreamKeySubjectRecorded`, appended atomically with `UpstreamKeyLinkRecorded` (the source fact of `UpstreamKeyLinked`, §4.1). The upstream login lives only in the Internal `UpstreamKeyDisplayIdentifierObserved`. Both are on the Account stream, inside the erasure boundary. A display identifier MUST NOT be used for matching, linking or authorization; it serves display and the `preferred_username` claim only, under [contract v1 §5.1](identity-contract-v1.md#51-display-claim-preferred_username). Upstream disclosure/retention is decided in §9 point 5. Raw link proofs, administrator evidence and free-text reasons MUST NOT enter events; store protected evidence separately and include only a non-redeemable EvidenceId.

Account streams use the account subject as Chronicle's default compliance subject. Chronicle cannot encrypt the event-source id: MUST NOT put `[PII]` or redundant `[Subject]` on it. If a non-account stream ever contains personal data, it MUST explicitly resolve the owning account through `Cratis.Chronicle.SubjectAttribute`/the append subject and prove a single-person erasure boundary; referencing an account does not itself redirect compliance. Do not mix different people's PII in one event/read-model document. This catalogue keeps personal profile/link values on Account streams.

Erasure MUST revoke grants, families, sessions and PATs first through the revocation protocol, then destroy all subject key revisions in every relevant namespace, remove projections/caches and propagate deletion to public-event consumers. Chronicle exposes `IEventStore.PII.DeleteEncryptionKeyFor(...)`; it records a monotonic fence before deletion, leaves unreadable ciphertext and refuses subsequent protected appends for that subject. Partial erasure (`EncryptionKeyErasureIncomplete`) or refused key purge MUST be retried/reported failed, never complete. Identity MUST NOT call `AllowNewEncryptionKeyFor` to bypass a tombstone or restore an erased account.

Crypto-shredding does not erase exported plaintext, logs, consumer databases, backups or restored key copies. Required key-store purge/consistency guarantees are deployment prerequisites. Restore MUST reapply erasure fences before serving; exports and backups need a documented retention boundary and consumer acknowledgements. `AccountErased` means the approved workflow boundary is complete, not universal physical deletion. `AccountTombstoned` reserves the subject forever without profile or upstream identifiers. The workflow facts and completion boundary are decided in §9 point 5.

## 6. Forbidden content and executable rule-check contract

### 6.1 Content rule

No event, internal or public, may contain a secret or redeemable value: access/ID/refresh tokens, PAT values, authorization/device/user codes, passwords/verifiers, client secrets, recovery codes, private/signing/encryption/Data Protection keys, session cookies, assertions or bearer/signed URLs. Encoding, encryption, nesting, a misleading property name or `[PII]` does not exempt them. Events may carry non-redeemable ids, approved one-way digests and public key thumbprints, **never raw key containers**. Signing-key events contain ids/thumbprints only; even public passkey material stays in a referenced protected credential store (§9 point 6).

Secret-derived hashes MUST use a reviewed keyed construction; the decided construction is HMAC-SHA-256 with a separate secret-store-held key, domain separation by purpose and an explicit hash-key revision. Hashing MUST happen before append, never in a reactor. An unkeyed hash of a low-entropy code/secret is forbidden because offline enumeration recovers it. Password verifiers remain operational credential state even if salted/keyed. Public-key thumbprints MAY use SHA-256 under a documented canonical public-key representation (for JWKs, RFC 7638). These digest choices/allowances are decided in §9 point 6.

### 6.2 Required executable check (W2)

The check is **implemented with the contracts project ([#35](https://github.com/Cratis/Identity/issues/35)); the build-failing content check is tracked in [#82](https://github.com/Cratis/Identity/issues/82)**. The data-only vectors are exercised by unit specs through the same engine as the build-time reflection check. The proposed exact check policy is open point 6:

1. Scan the built `Cratis.Identity.Contracts` assembly, every type in namespace `Cratis.Identity.Contracts` or its descendants that is registered as an event: `[EventType]`, `[EventTypeGenerationFor<T>]`, or explicitly registered artifacts. Include Public/Internal types and **every** generation, not only exported CLR types. Compare reflection/registration discovery to the catalogue and fail on missing/unclassified types, ids shared by distinct event kinds, duplicate `(id, generation)` registrations or any Identity event registered outside that namespace/assembly. Registration must not bypass the check. Reachable payload types from other assemblies are scanned recursively too.
2. Walk every serialized instance property, including inherited/nested members, concept values, collection elements, map keys/values and actual wire-name aliases. Check both CLR and wire names at each path. Use a visited-type set to terminate cycles, not to skip reachable members. Nullable wrappers do not exempt a value. Fail open-ended `object`, `dynamic`, `System.Text.Json.JsonElement`, `JsonDocument`, `JsonNode`, `Newtonsoft.Json.Linq.JToken` (including derived types), untyped dictionaries/extension data and polymorphic payloads without a closed, recursively checked case set. A primitive dictionary with arbitrary field names also fails: dynamic names cannot bypass name checks.
3. Normalize names by removing non-ASCII-alphanumeric characters and ASCII-lowercasing. A name containing any of `token`, `code`, `password`, `passwd`, `pwd`, `secret`, `key`, `credential`, `assertion`, `cookie`, `nonce`, `private`, `certificate`, `recovery`, `personalaccesstoken`, or `patvalue` is forbidden. Exact names `pat` and `state` are also forbidden. This deliberately conservative match includes `AuthorizationCode`, `AccessToken`, `RefreshToken`, `ClientSecret`, `KeyMaterial`, `PrivateKey`, `RecoveryCodes` and `PATValue`. Apply the same matching to domain concept type names before unwrapping; generic collection wrappers are checked through their elements.
4. Independently forbid the following types, their derived/implementing types, arrays/collections of them and concepts wrapping them, regardless of name: `byte[]`, `Memory<byte>`, `ReadOnlyMemory<byte>`, `ArraySegment<byte>`, `Span<byte>`, `ReadOnlySpan<byte>`, `System.IO.Stream`, `System.Security.SecureString`, `System.Security.Cryptography.AsymmetricAlgorithm`, `SymmetricAlgorithm`, `RSAParameters`, `ECParameters`, `System.Security.Cryptography.X509Certificates.X509Certificate`, `Microsoft.IdentityModel.Tokens.SecurityKey`, `JsonWebKey`, `SigningCredentials`, `EncryptingCredentials`, `SecurityToken`, `System.IdentityModel.Tokens.Jwt.JwtSecurityToken`, `Microsoft.IdentityModel.JsonWebTokens.JsonWebToken`, `System.Security.Claims.ClaimsPrincipal`, `ClaimsIdentity` and `System.Net.NetworkCredential`. Bytes/crypto containers and token/credential objects cannot be exempted by renaming to `Data` or `KeyThumbprint`.
5. A suffix `Id`, `Hash` or `Thumbprint` is **not a blanket exemption**. To override step 3 only, the leaf MUST use an entry in the contracts project's reviewed safe-concept registry, unwrapping to string/UUID, with no additional serialized fields. A valid leaf exception covers both that member's name and its approved concept type's name (for example `TokenHash` of `SecretDigest`); the primitive value is not a second unchecked payload. `*Id` entries must be generated/non-redeemable references (not token handles, subjects disguised as ids, URLs containing secrets or encoded material). `*Hash` entries must attest HMAC-SHA-256, purpose and key revision; their producing-writer specs MUST verify the computation and keyed treatment of low-entropy inputs. `*Thumbprint` entries must attest the canonical SHA-256 public-key digest. Never exempt password/verifier concepts. Step 4 always wins. Step 7 below still applies.
6. Initial exception examples are `TokenHash`/`ClientSecretHash` using `SecretDigest` (keyed), `KeyThumbprint` using `PublicKeyThumbprint` (public-key digest), and `SigningKeyId`/`LinkedKeyId` using `OpaqueReferenceId` (non-redeemable UUID reference). `FactId` (§4.1) contains no forbidden term, so it needs no registry entry; it MUST still be a generated, non-redeemable UUID. Other catalogue references using these concepts follow the same checks. All exceptions need a checked-in purpose/producer justification; an unknown registry entry fails. Metadata accompanying a digest is trusted configuration/spec evidence, not a caller-provided claim that a string is safe. A reflection check cannot establish entropy or detect a deliberately mislabelled secret; executable writer specs and review MUST close that gap.
7. Independently require effective `[PII]` for email, personal name/login and upstream subject: match normalized leaf names `email`, `emailaddress`, `name`, `displayname`, `fullname`, `givenname`, `familyname`, `login`, `loginname`, `idpsub`, `upstreamsubject`, `upstreamsubjectid`, or concept types `Email`, `PersonalName`, `UpstreamSubject`. Resolve markers on property, declaring type, property concept or matching constructor parameter as Chronicle does. Require a resolvable single account compliance subject. Non-person business labels MUST use distinct reviewed concepts/names, not a blanket `Name` exemption. `idp_sub` without `[PII]` fails even though it is neither a token nor a secret. On a **Public** event, an upstream subject or login leaf fails even when it is marked `[PII]` (`public_upstream_identifier`); only Internal Account-stream events may carry them (§5).

Reason ids for the vectors are `forbidden_member_name`, `forbidden_payload_type`, `open_payload_shape`, `unapproved_safe_concept`, `unkeyed_secret_hash`, `redeemable_reference`, `password_verifier_forbidden`, `missing_pii` and `unresolved_compliance_subject`. Return all distinct applicable path/reason pairs, sorted by path then reason. Resolve closed-shape/type prohibitions first; a forbidden type reports `forbidden_payload_type` rather than a redundant name error at that leaf. For a name exception, report its specific failed constraint instead of `forbidden_member_name`; a missing registry entry reports `unapproved_safe_concept`. PII/subject checks remain independent. Additional catalogue/registration/migration failures need separately documented reason ids in #35.

The ordinary solution build runs `Identity.Contracts.Check` after compiling Contracts and propagates a nonzero result as an MSBuild failure. Reflected schemas and the locked vectors share one content-rule engine. Unit specs compare the embedded catalogue mirror to §8 and §4.1. See the [checker guide](../../Source/Identity.Contracts.Check/README.md) for invocation, diagnostics, catalogue registration, and reviewed safe-concept entries. This check covers declared content and registration for currently allocated contracts; it does not claim runtime hashing, writer authorization, or erasure-workflow enforcement.

A violation MUST fail the contracts build through its executable spec gate, with event identity/generation, full member path and stable reason id. The implementation MUST run as a required build/CI step, not an optional manual test. Diagnostics MUST NOT print values. Name/type matching is a guardrail, not permission to append any value that happens to evade it. Tests MUST cover nested values, aliases, type disguises and producer behavior as well as these vectors.

## 7. Facts versus operational state

Accounts, links, accepted client configuration, resources/scopes, consent/grants, meaningful family lifecycle and Access configuration are durable facts. The catalogue replaces the responsibilities visible today in `Accounts`/`AccountDocument`, `GitHubSignInEvents`, `ClientRegistrations`/`ClientDescriptors`, `Consent`, `GrantRevocation` and the refresh-reuse handlers, not their present storage schemas. `ConfiguredTenantDirectory` is a development fixture; product-owned live membership is not replaced by Identity events.

D18 excludes individual refresh rotations, codes, `state`/nonce, `jti`/replay caches, token redemption, CIMD fetch caches, rate counters and failed sign-in attempts. Browser sessions, tenant selection and AuthProxy on-behalf session state are operational, **not session events**. No access tokens are stored. Key material belongs to the host's secret store/KMS.

Revocation/disable fences, tombstones, account/grant epochs and pending outbox entries are authoritative operational state, durably committed before acknowledgement with the independently retained fence log. The corresponding §8 events report completed business facts asynchronously; they are not the fence itself and their projection is never a prerequisite to refusing revoked credentials. `FenceId`, `FenceSequence`, epochs and outbox-derived event identity permit idempotent recovery; replay MUST NOT resurrect credentials. Names/fields marked **align with identity-revocation-v1** follow [identity-revocation-v1](identity-revocation-v1.md) (§9 point 7).

Access projection freshness/heartbeat transport is operational, not a periodic domain event in this catalogue. A policy change with a new epoch is a domain fact. D20's freshness/epoch-before-policy protocol remains a separate contract; this catalogue MUST NOT manufacture authority from a transport heartbeat.

## 8. Event catalogue v1

Each row is one event kind; no row permits arbitrary fields. Type ids are allocated under §2.3 before implementation appends. Areas are namespace suffixes. Stream aliases expand to exact event-source-id rules in §2.2. `†`/`‡` expand to §5; other fields are non-PII ids, bounded configuration, closed reason ids or instants. `AccountSubject` is a referenced contract v1 subject, never an upstream id. `KeyHash`/`ClientSecretHash` include digest purpose/key revision through approved schema metadata or explicit companion fields (§6). Writers mean the sole authoritative module; for a Public row the writer appends the row's Internal source fact and the contract is published to the outbox under §4.1.

All names, fields, writer assignments and Public/Internal choices are decided in §9 point 3. Access rows additionally follow D5, decided as option A in Identity #23 (§9 point 8); no row grants Access ownership of product membership, roles, invitations or delegation approvals.

| Event | Type id | Area | Public/Internal | Event source id | Fields (PII marks) | Written by | Notes |
|---|---|---|---|---|---|---|---|
| AccountRegistered | fd1cb0b5-17c1-4f34-b625-c318dc928d97 | Accounts | Public | Account | RegisteredAt | Identity | Profile and initial verified key are separate facts in the same registration workflow. |
| AccountEmailChanged | 92fde774-1586-4d45-a354-8349a9f5e3fd | Accounts | Internal | Account | Email† | Identity | No profile snapshots; protected concept from generation 1. |
| AccountNameChanged | 2eba996c-8d19-4d5c-9648-3dca02daf879 | Accounts | Internal | Account | PersonalName† | Identity | Personal name concept. |
| AccountDisabled | 8e1e68fd-e4fb-456a-9af4-f95185be950b | Accounts | Public | Account | ReasonId, AccountEpoch, FenceId, FenceSequence | Identity | align with identity-revocation-v1; fence precedes acknowledgement. |
| AccountEnabled | 4fa5a4be-c0e1-4ad1-81d4-4eb304aab74d | Accounts | Public | Account | AccountEpoch, EnabledAt | Identity | Does not restore old grants or remove a tombstone. |
| AccountTombstoned | 6d6b4f99-5ac3-4493-b892-13a5e44c6518 | Accounts | Public | Account | TombstonedAt | Identity | Permanent subject reservation; no upstream/profile fields. |
| AccountErasureRequested | assigned at implementation | Accounts | Internal | Account | ErasureId, RequestedAt | Identity | Accepted user/legal request, not a retry signal. |
| AccountErasureFailed | assigned at implementation | Accounts | Internal | Account | ErasureId, StageId, ReasonId | Identity | Real failed workflow transition; no error payload or repeating retry events. |
| AccountErased | assigned at implementation | Accounts | Public | Account | ErasureId, CompletedAt, BoundaryVersion | Identity | Consumer cleanup acknowledgements and approved completion boundary (§5). |
| UpstreamKeyLinkProposed | b49850a5-1179-489b-bd37-96a4bb25ff69 | Accounts | Internal | Account | LinkProposalId, ConnectionId, IdpIssuer, IdpSub† | Identity | Accepted intent; pending_proof, never provisional access. |
| UpstreamKeyLinkQuarantined | a407f6de-7435-47b3-bcc8-ae3498010e71 | Accounts | Internal | Account | LinkProposalId, ReasonId, EvidenceId, HoldNotBefore | Identity | Proof/conflict transition; hold and fresh proof per contract v1. |
| UpstreamKeyLinkCleared | 0f4ff8c1-a7e4-48e4-9bd1-65abbe2b465d | Accounts | Internal | Account | LinkProposalId, ResolutionId, EvidenceId, ReasonId | Identity | Authorized resolver's clearance, not waived proof or uniqueness. |
| UpstreamKeyLinked | e2d1b4ad-914e-41c6-9187-c589fb36d68c | Accounts | Public | Account | LinkId, ConnectionId, IdpIssuer, KeyHash‡, EvidenceId, LinkedAt | Identity | Atomic uniqueness, enforced on its source fact `UpstreamKeyLinkRecorded` (§4.1); first registration or confirmed post-hold proposal. No raw upstream id (contract v1 §7). Proof material excluded. |
| UpstreamKeySubjectRecorded | b0f49e52-c457-484b-adf8-8a2207a36869 | Accounts | Internal | Account | LinkId, IdpIssuer, IdpSub† | Identity | Appended atomically with `UpstreamKeyLinkRecorded`, the source fact of `UpstreamKeyLinked`; the confirmed link's raw `IdpSub` (only Internal events carry it, see §5). Inside the erasure boundary. |
| UpstreamKeyDisplayIdentifierObserved | ecb91993-88b6-42a1-bdbc-ee644bd50fca | Accounts | Internal | Account | LinkId, Login†, ObservedAt | Identity | Per key; mutable upstream login for display and `preferred_username` only. Never used for matching, linking or authorization. Inside the erasure boundary. |
| UpstreamKeyLinkRejected | 81cf15b3-1e52-441d-b55c-0b89853f8861 | Accounts | Internal | Account | LinkProposalId, ReasonId | Identity | Terminal proposal; retry needs a new proposal/proofs. |
| UpstreamKeyUnlinked | f84b03e3-e0c1-4593-962e-917c56e50bcc | Accounts | Public | Account | LinkId, KeyHash‡, ReasonId, UnlinkedAt | Identity | New fact, never a rewrite/transfer of the original link. Reports, asynchronously, the unlink fence that revokes every grant, refresh family and session authenticated through that key (identity-revocation-v1 §7 Unlink); the event is not the fence itself. The unlinked key stays reserved to its old account; relinking it to any account needs an explicit reviewed operation (its own catalogue extension), never automatic linking. |
| PasskeyRegistered | assigned at implementation | Accounts | Internal | Account | PasskeyId, KeyThumbprint, RegisteredAt | Identity | Credential-store reference/thumbprint only; no attestation or key blob. |
| PasskeyRemoved | assigned at implementation | Accounts | Internal | Account | PasskeyId, RemovedAt | Identity | No authenticator secret or recovery code. |
| ConsentGranted | assigned at implementation | Authorization | Internal | Account | ConsentId, ClientRegistrationId, ResourceId, InstallationId, TenantId, EnvironmentId, Scopes, GrantedAt, ExpiresAt | Identity | Accepted bounded consent, not browser form/proof state. |
| ConsentWithdrawn | assigned at implementation | Authorization | Public | Account | ConsentId, ReasonId, WithdrawnAt | Identity | Related grants revoked through fences; align with identity-revocation-v1. |
| AuthorizationGranted | assigned at implementation | Authorization | Public | Grant | AccountSubject, ConsentId, ClientRegistrationId, ResourceId, InstallationId, TenantId, EnvironmentId, Scopes, PolicyEpoch, GrantedAt, ExpiresAt | Identity | Scope ceiling, not live membership; grant creation does not mint on replay. |
| AuthorizationRevoked | assigned at implementation | Authorization | Public | Grant | AccountSubject, ReasonId, GrantEpoch, FenceId, FenceSequence | Identity | align with identity-revocation-v1. |
| RefreshTokenFamilyStarted | assigned at implementation | Authorization | Internal | Family | GrantId, AccountSubject, StartedAt, AbsoluteExpiresAt | Identity | Family lifecycle only; no initial/rotated token or handle. |
| RefreshTokenFamilyRevoked | assigned at implementation | Authorization | Public | Family | GrantId, ReasonId, FenceId, FenceSequence | Identity | align with identity-revocation-v1; durable fence first. |
| RefreshTokenReuseDetected | assigned at implementation | Authorization | Internal | Family | GrantId, DetectionId, DetectedAt | Identity | Meaningful compromise fact, no submitted token; align with identity-revocation-v1. |
| ClientRegistered | assigned at implementation | Clients | Internal | Client | ClientId, RegistrationKind, RegisteredAt | Identity | Pre-registered/CIMD/DCR only as permitted by policy; ClientId is validated, non-redeemable. |
| ClientMetadataObserved | assigned at implementation | Clients | Internal | Client | MetadataHash, RedirectUris, ObservedAt | Identity | New accepted metadata revision only, not each fetch/cache hit; URLs MUST exclude secrets/PII. |
| ClientRedirectUrisChanged | assigned at implementation | Clients | Internal | Client | RedirectUris | Identity | Accepted redirect set; no signed/redeemable URLs. |
| ClientSecretRotated | assigned at implementation | Clients | Internal | Client | ClientSecretId, ClientSecretHash, ActivatedAt, PreviousValidUntil | Identity | High-entropy keyed hash, no secret; initial secret uses this fact too. |
| ClientRetired | assigned at implementation | Clients | Internal | Client | ReasonId, RetiredAt | Identity | New fact replaces store deletion; associated grants need revocation. |
| SigningKeyActivated | assigned at implementation | Signing | Public | Signing key | KeyThumbprint, Algorithm, ActiveFrom | Identity | KMS/secret-store custody; no public/private key container or certificate. |
| SigningKeyRetired | assigned at implementation | Signing | Public | Signing key | KeyThumbprint, RetiredAt | Identity | Overlap/verification policy is not key disclosure. |
| ResourceRegistered | assigned at implementation | Resources | Public | Resource | ResourceIndicator, Audience, ScopePrefix | Identity | Validated contract v1 catalog values. |
| ResourceScopeRegistered | assigned at implementation | Resources | Public | Resource | ScopeName | Identity | One accepted scope fact. |
| ResourceScopeRetired | assigned at implementation | Resources | Public | Resource | ScopeName, RetiredAt | Identity | No implicit widening/remapping of old grants. |
| OrganisationRegistered | assigned at implementation | Access | Public | Organisation | RegisteredAt | Access | per D5 (Identity #23). |
| OrganisationPolicyChanged | assigned at implementation | Access | Public | Organisation | PolicyEpoch, SsoRequired, MinimumAcr, PatAllowed, EffectiveAt | Access | per D5 (Identity #23); real change only; D20 epoch-before-policy applies. |
| OrganisationDomainVerified | assigned at implementation | Access | Public | Organisation | Domain, VerificationId, VerifiedAt | Access | per D5 (Identity #23); no DNS challenge code/value. |
| TenantBindingProposed | assigned at implementation | Access | Internal | Binding | OrganisationId, ResourceId, InstallationId, TenantId, EnvironmentId | Access | per D5 (Identity #23); no membership or authority until both sides confirm. |
| TenantBindingConfirmed | assigned at implementation | Access | Public | Binding | OrganisationId, ResourceId, InstallationId, TenantId, EnvironmentId, OrganisationConfirmationId, ProductConfirmationId | Access | per D5 (Identity #23); authenticated confirmations, not bearer confirmation links. |
| TenantBindingRemoved | assigned at implementation | Access | Public | Binding | OrganisationId, PolicyEpoch, ReasonId, RemovedAt | Access | per D5 (Identity #23); restricting change; align with identity-revocation-v1. |
| EnterpriseConnectionRegistered | assigned at implementation | Access | Public | Connection | OrganisationId, IdpIssuer, Protocol | Access | per D5 (Identity #23); validated authority, no federation secret/certificate blob. |
| ConnectionAssuranceMappingChanged | assigned at implementation | Access | Public | Connection | MappingRevision, AssuranceMappings | Access | per D5 (Identity #23); closed typed mapping, unknown evidence yields basic. |
| RealmPolicyEpochAdvanced | assigned at implementation | Revocation | Public | Realm | PolicyEpoch, ReasonId, FenceId, FenceSequence | Identity | align with identity-revocation-v1; policy fact after durable fence, not a recovery invalidation or heartbeat. |
| RealmRecoveryEpochAdvanced | assigned at implementation | Revocation | Public | Realm | RestoreEpoch, ReasonId, FenceId, FenceSequence | Identity | Independently durable recovery epoch after any restore or owner-initiated hard invalidation; separate from policy. `RestoreEpoch` carries the recovery epoch; the field avoids the forbidden `recovery` substring (§6.2 step 3). Reports the fact; enforcement MUST NOT wait on event delivery. |

A field whose name triggers §6 (for example `KeyHash`, `PasskeyId`, `ClientSecretId` or `KeyThumbprint`) MUST have a reviewed concept/exception. No exception is needed for `PolicyEpoch` or `ScopeName` unless their underlying types trigger the rule. No table entry overrides the rule check. `AssuranceMappings` MUST be a closed collection of typed source/target pairs, never arbitrary JSON or credential claims. Hash-purpose/key-revision fields MUST themselves be non-secret references. Additional Access/provisioning facts require a separately reviewed catalogue extension; product roles, invitations, runner credentials and delegation events belong to their own writers, not this catalogue.

## 9. Decisions recorded at sign-off

Decided in Identity #14 (2026-11-05), with the recommended answers from the sign-off package:

1. **Naming/areas, both sets' placement in Contracts and explicit id allocation/registry:** §2 vocabulary, both sets in Contracts, UUID-v4 ids allocated by implementer and recorded in §8 before first append.
2. **Non-account stream ids, realm scoping and reference boundaries:** §2.2 opaque streams; account subject unchanged from contract v1; no PII-derived stream ids.
3. **Initial names, fields, writers and public/internal consumer surface:** exactly §8; profile/proposal/client details internal, account/link/grant/revocation and required configuration facts public.
4. **Stricter generation policy and observer rollout requirements:** every persisted schema change gets a consecutive generation, tested bidirectional chains retained forever; pin supported consumer generations after version-qualified delivery tests.
5. **Upstream disclosure, lookup-digest PII treatment and erasure boundary:** §5 minimum approved recipients, protected lookup digests, consumer acknowledgements plus documented export/backup retention before AccountErased.
6. **Hash policy, key-reference-only events and exact fail-build rule/exception registry:** §6 HMAC-SHA-256 for secret hashes, canonical SHA-256 thumbprints, no raw key containers, closed recursive scan and reviewed typed exceptions in #35.
7. **Revocation event vocabulary and separation from operational fences:** §8 names/fields aligned with [identity-revocation-v1](identity-revocation-v1.md); no session/rotation/fence-transport events.
8. **D5-dependent Access ownership, binding identity and policy fields:** §8 Access writer with bilateral binding confirmation; organisation syntax/scope and connection ownership follow D5 (decided as option A, Identity #23); membership remains product-owned.

This specification does not itself decide D20 (option C, Identity #69), whose epoch/freshness protocol is a separate contract, nor the revocation rules, which are in [identity-revocation-v1](identity-revocation-v1.md).

## 10. Data vectors

[Vectors](../../Source/Identity.Specs/Vectors/identity-events-v1/) contain named cases with `given` (event shape, concept-registry evidence and subject context), `when.action` (`check_event_contract`) and `then` (pass/fail and stable reason ids). Each vector file's `context` supplies the assembly, namespace, event marker, generation and default compliance subject for every case; `given` overrides those defaults (including an explicit null subject), and supplies the event name and member tree. Type ids are synthetic check inputs, not registry allocations. Shape descriptors represent serialized members; `pii` denotes an effective Chronicle marker, not a runtime boolean field. `registry` entries describe approved producer evidence, not event payloads. Synthetic examples contain no real secrets. The future check MUST satisfy each case and supplement it with reflection, producer, migration and build-failure specs in #35. JSON parsing alone proves data syntax, not runtime conformance.

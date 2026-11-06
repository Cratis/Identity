# Identity module outline

```text
Doc id: identity-module-outline
Version: 1
Status: Proposed
Issue: https://github.com/Cratis/Identity/issues/11
Depends on: identity-design v1, identity-platform-corrections v1 (Proposed), identity-package-and-storage v1 (Signed off), identity-events v1 (Signed off), identity-contract v1 (Signed off)
```

## 1. Scope and authority

This note is the target outline of the `Cratis.Identity` module: which behaviors are Arc commands, queries and Chronicle events, which state stays operational, how OpenIddict sits on top, and what the module guarantees about read-your-writes, realm isolation and authorization. It is the conformance anchor against which each delivery step is checked. It is not a specification: it adds no event, claim or contract, and it never overrides a signed-off or Locked document.

Binding inputs, in this order:

1. The headless module direction, as recorded in [Identity design: binding excerpts](identity-design.md) (W0 identity design, final, approved 2026-11-05): Identity ships commands, queries and events only; every application builds its own UI; state is event-sourced in Chronicle except where the storage design says otherwise; the host adds the module next to its own Arc registration, as Usher does.
2. [Identity package, storage and host UI design](identity-package-and-storage-v1.md), §2 store inventory, §4 packages and host surface, §6 migration, §7.
3. [Identity event rules and catalogue](../specs/identity-events-v1.md): §2.2 stream ids, §4.1 source facts and outbox, §8 catalogue.
4. [Identity contract](../specs/identity-contract-v1.md) and [revocation protocol](../specs/identity-revocation-v1.md).

Catalogue event types and type ids are unchanged. The access-token claim set is unchanged. Public contracts reach consumers only through the outbox reactors (identity-events §4.1).

### Design references

| Reference | Used for |
|---|---|
| [Accounts and linked keys](identity-design.md#accounts-and-linked-keys) | Accounts and linked upstream keys as event-sourced facts; never link by email |
| [Who writes which fact](identity-design.md#who-writes-which-fact) | One authoritative writer per fact |
| [Headless event-sourced module](identity-design.md#identity-as-a-headless-event-sourced-module), [packages](identity-design.md#module-packages), [surface](identity-design.md#module-surface) | The module shape: commands, queries and events; OAuth endpoints are the one protocol exception |
| [What is event sourced](identity-design.md#what-is-event-sourced) | The store table: what stays operational and what is never in Chronicle |
| [Revocation protocol](identity-design.md#revocation-protocol) | Fence, outbox and recovery (step S6) |
| [UI moves out](identity-design.md#ui-moves-out-the-security-does-not) | No pages in the module; the consent security stays in the package |
| [Realm consequences](identity-design.md#realm-consequences) | A realm is a host with its own event store |
| [Hard-to-change contracts](identity-design.md#hard-to-change-contracts) | Person id and event rules that this outline must not change |
| identity-package-and-storage v1 §2, §4, §6, §7 | Store inventory, package and host surface, migration |
| identity-events v1 §2.2, §4.1, §8 | Stream ids, source facts and outbox, catalogue |
| Identity [#20](https://github.com/Cratis/Identity/issues/20) | The issuer and security owner role used in section 8 |
| `Showrunner/Source/DotNET/Usher` | Packaging shape: `UsherHostApplicationBuilderExtensions`, `UsherServiceCollectionExtensions` (`TryAdd*`), `Identity/ShareIdentity/ShareIdentity.cs` (command, validator and event in one file, server-owned identity through `ISignedInReporter`) |
| `Showrunner/Integration/given/WebApplicationWithUsher.cs`, `ChronicleInfrastructure.cs` | The embedding host and the Chronicle kernel test container |
| `Chronicle/Source/Kernel/Server/Authentication/OpenIddict` | Precedent for OpenIddict stores that read Chronicle read models |

## 2. Conventions

Application profile. A feature folder holds one folder per slice, and each slice holds a single `<Slice>.cs` with the command and its validator, constraints, slice-local events, read models with their static queries, projections and reactors. Catalogue events stay in `Cratis.Identity.Contracts`. Specs mirror the slice path in `Source/Identity.Specs/<Feature>/<Slice>/when_<behavior>/and_<scenario>.cs`; spec projects stay `<Source>.Specs`. Namespaces are `Cratis.Identity.<Feature>.<Slice>`. Event source ids follow identity-events §2.2.

Delivery is in steps, each independently shippable and each deleting the path it replaces in the same change.

**Entry gate for S1.** The deletion of the MongoDB account path (`Accounts/Accounts.cs`, `AccountDocument.cs`, `IAccounts.cs`, `EventSourcedAccounts.cs` and the `EventSourced` opt-in) waits for the answer to the question on [#1](https://github.com/Cratis/Identity/issues/1): does any deployment hold Identity accounts? The [appendix](#deployments-as-of-2026-11-06) shows that none was found in the sources that could be read. If the answer is "no deployment", S1 deletes the path without a migration. If any deployment holds account documents, S1 also delivers an id-preserving import (a one-shot hosted service that appends the existing account events under each document's existing account subject, so `sub` claims do not change) and that deployment runs it before its MongoDB account code is removed. S1 implementation may start before the answer; the deletion may not merge before it.


| Step | Delivers |
|---|---|
| S1 | Accounts and sign-in through a command; the Mongo account path is deleted |
| S2 | Linking commands, lookup by key, guard fix |
| S3 | Clients and resources by commands; OpenIddict application and scope stores over read models |
| S4 | Headless consent and sign-out; UI moves to the reference host |
| S5 | Account lifecycle commands |
| S6 | Revocation (fence, outbox, recovery) and refresh-family facts |
| S7 | Cleanup and publish readiness |

## 3. Event-sourced slices

Chronicle, realm event store, Default namespace pinned per section 7, read-your-writes per section 6.

| Feature / slice folder | Commands (Arc `[Command]`) | Events appended (Contracts) | Read models / queries | Projections / reactors | Step |
|---|---|---|---|---|---|
| `Accounts/SigningIn/SigningIn.cs` | `SignInWithUpstreamKey(FactId OperationId)`, **protocol-only** (section 5); `Handle(IVerifiedUpstreamSignIn verified, …)` returns `(IEnumerable<EventForEventSourceId>, AccountSubject)` | new key: `AccountRegistrationRecorded`, `UpstreamKeyLinkRecorded`, `UpstreamKeySubjectRecorded`, `UpstreamKeyDisplayIdentifierObserved`; known key: `AccountEmailChanged`, `AccountNameChanged`, `UpstreamKeyDisplayIdentifierObserved` | uses `UpstreamKeyLookup` (decision read) | `UniqueUpstreamKey` constraint | S1 |
| `Accounts/Account.cs` (feature-level read model used by the protocol and tenants) | n/a | n/a | `[ReadModel] Account(AccountSubject Id, PersonalName Name, Email? Email, long GitHubId, Login GitHubLogin, AccountStatus Status)` and query `Account.Mine` (current session subject) | projection from the account events above and `AccountDisablementRecorded`, `AccountEnablementRecorded`, `AccountTombstoneRecorded` | S1 |
| `Accounts/Linking/ProposingLink`, `ConfirmingLink`, `RejectingLink`, `ClearingQuarantine`, `Unlinking` | `ProposeLink`, `ConfirmLink`, `RejectLink`, `ClearLinkQuarantine`, `UnlinkUpstreamKey` (`[Authorize]`; the subject comes from the session as in Usher's `ISignedInReporter`; the verified upstream proof is server-owned, section 5) | `UpstreamKeyLinkProposed`, `UpstreamKeyLinkQuarantined`, `UpstreamKeyLinkCleared`, `UpstreamKeyLinkRecorded`, `UpstreamKeySubjectRecorded`, `UpstreamKeyLinkRejected`, `UpstreamKeyUnlinkRecorded` | `Accounts/Linking/LinkedKeys/LinkedKeys.cs`: the current `LinkedAccountKeys.ForCurrentAccount` | `AccountLinkStateReducer`, `AccountGuardProjection` (kept as decision read models) | S2 |
| `Accounts/Lookup/` | n/a | n/a | `UpstreamKeyLookup` **keyed by `UpstreamLookupDigest`**, read with `GetInstanceById` (no scan of all instances) | `UpstreamKeyLookupProjection`, re-keyed | S2 |
| `Accounts/Lifecycle/DisablingAccount`, `EnablingAccount`, `TombstoningAccount` | `DisableAccount`, `EnableAccount`, `TombstoneAccount` | `AccountDisablementRecorded`, `AccountEnablementRecorded`, `AccountTombstoneRecorded` | `Account.Status` | fences via S6 once they exist | S5 |
| `Accounts/Publishing/` | n/a | outbox: `AccountRegistered`, `AccountDisabled`, `AccountEnabled`, `AccountTombstoned`, `UpstreamKeyLinked`, `UpstreamKeyUnlinked` | n/a | `AccountPublications` reactor (kept, moved) | S2 |
| `Clients/Registering`, `ChangingRedirectUris`, `RotatingSecret`, `Retiring` | `RegisterClient`, `ChangeClientRedirectUris`, `RotateClientSecret` (returns the secret once; the event holds only the keyed digest), `RetireClient`; all `[Authorize(Policy = IdentityPolicies.IssuerSecurityOwner)]`, deny by default (section 8) | `ClientRegistered`, `ClientRedirectUrisChanged`, `ClientSecretRotated`, `ClientRetired` (type ids allocated at implementation per identity-events §2.3) | `Clients/Listing/Listing.cs`: `[ReadModel] ClientRegistration` keyed by `ClientRegistrationId` and the `AllClients` query; `ClientByClientId` keyed by the OAuth `client_id` (used by the store adapter) | projections | S3 |
| `Resources/Registering`, `RegisteringScope`, `RetiringScope` | `RegisterResource`, `RegisterResourceScope`, `RetireResourceScope`; all `[Authorize(Policy = IdentityPolicies.IssuerSecurityOwner)]`, deny by default (section 8) | `ResourceRegistered`, `ResourceScopeRegistered`, `ResourceScopeRetired` | `Resources/Listing`: `[ReadModel] Resource` (audience, indicators, display name) and `ResourceScope` keyed by scope name; replaces the configuration-backed `ResourceCatalog` | projections | S3 |
| `Consent/Deciding/Deciding.cs` ([#78](https://github.com/Cratis/Identity/issues/78)) | `ApproveConsent(ConsentRequestId)`, `DenyConsent(ConsentRequestId)` (`[Authorize]`; the session must match the pending request; compare-and-set transitions with an activation barrier, [section 9](#s4-acceptance-the-grant-activation-barrier)) | `ConsentGranted`, `AuthorizationGrantRecorded` (source fact of `AuthorizationGranted`), appended before the grant becomes usable | `Consent/Pending/Pending.cs`: `PendingConsent` query reads the **operational** pending-request record, never Chronicle (identity-package-and-storage §2) | n/a | S4 |
| `Consent/ConnectedApps/` | `WithdrawConsent` | `ConsentWithdrawalRecorded` | `ConnectedApp` read model and `MyConnectedApps` query | projection; revocation reactor via S6 | S4 (read), S6 (withdraw) |
| `Authorizations/Revoking` ([#41](https://github.com/Cratis/Identity/issues/41)) | `RevokeAuthorization` | `AuthorizationRevocationRecorded`, `RefreshTokenFamilyRevocationRecorded` via the fence outbox | n/a | outbox dispatcher appends after the operational fence | S6 |
| `Authorizations/RefreshFamilies/RecordingFamilyStart`, `RecordingReuse` | `RecordRefreshTokenFamilyStart`, `RecordRefreshTokenReuse`, both **protocol-only** (section 5), executed after the operational store committed the first refresh-token issuance for a grant or the reuse revocation | `RefreshTokenFamilyStarted`, `RefreshTokenReuseDetected` (identity-events §8; Internal; event source is the `FamilyId` per §2.2; type ids assigned at implementation; no token value or handle) | feed `ConnectedApp` (S4 read side) and audit | appended through the S6 revocation outbox, so a crash between issuance and append does not lose the fact | S6 |
| `Sessions/SigningOut` | `SignOut` (`[Authorize]`; ends the browser session; no event) | none (session state is operational) | n/a | n/a | S4 |

Account erasure (`AccountErasureRequested`, `AccountErasureFailed`, `AccountErasureCompleted`), passkeys, organisations, tenant bindings, connections, realm epochs and signing-key facts stay in their existing issues and are outside this outline.

## 4. Not event-sourced, and why

The module exposes commands, queries and events, and everything that is a durable fact about people, clients, resources, consent and grants is event-sourced. The exceptions below are deliberate and each has a reason. They are listed so they can be confirmed or rejected one by one; anything not listed here is event-sourced.

| State | Where | Why it is not event-sourced | Notes |
|---|---|---|---|
| Authorization codes, device and user codes, individual refresh-token rotations, consumption compare-and-set, `state` and nonce, `jti` replay entries, CIMD fetch cache, rate counters | Operational store (TTL): OpenIddict token store and `Operational/` collections | Single-use or expiring protocol state. Correctness needs an atomic consume-once check and deletion at expiry; an immutable log would keep redeemable values forever and cannot express "consumed exactly once" without a separate lock anyway | Kept from today: OpenIddict MongoDB token store, `Storage/StorageIndexes.cs` TTL indexes, `Storage/TokenPruning.cs`, `Authorization/RejectExpiredTokenEntries.cs`. The durable facts (refresh-family started, reuse detected) are events (S6) |
| OpenIddict authorization records (live status), browser sessions, pending consent requests, tenant selection | Operational store | Live, short-lived protocol state that OpenIddict checks on every request; it must change atomically and expire. The decisions themselves are events | The Chronicle consent and grant facts feed connected apps and audit only |
| Revocation fences, tombstones, grant and account epochs, revocation outbox, fence log | Operational store and the independent fence log (S6, [#41](https://github.com/Cratis/Identity/issues/41), [#42](https://github.com/Cratis/Identity/issues/42)) | A revoke must hold on the next request even while projections lag, so the enforcement point cannot be a projection. The revocation itself is an event | Never read from projections (identity-package-and-storage §2) |
| Lookup-key revision guard | Operational store: one fixed-id record with atomic compare-and-set | A concurrency guard, not a fact | `LookupRevisionGuard` moves to `Operational/` with the atomic fix in S1 |
| Signing and encryption keys, Data Protection ring, upstream credentials, digest keys | Secret store or KMS (`Keys/`, kept) | Secrets must not sit in an immutable, replicated log: they must be rotatable and destroyable, and events are neither | Never in Chronicle; events refer to key ids only |
| Failed sign-in attempts | Logs and metrics (`[LoggerMessage]`) | Not a domain fact and unbounded in volume (anyone can produce them) | Only a rate limiter's counters belong in the operational TTL store |
| Access tokens | Not stored | Self-contained signed JWTs; nothing to store | The grant they come from is an event |

## 5. OpenIddict over Chronicle read models and the operational store

OpenIddict stays the protocol engine and owns discovery, JWKS, authorize, token, revoke and introspect. It is the one place where "only commands, queries and events" cannot hold literally. In `Hosting/IdentityServiceCollectionExtensions.cs`, `.AddCore(options => options.UseMongoDb())` is replaced by explicit stores:

- **Application store**: `Protocol/Stores/ClientApplicationStore : IOpenIddictApplicationStore<ClientRegistration>`. Read-only over Chronicle: `FindByClientIdAsync` reads `ClientByClientId`, `FindByIdAsync` reads `ClientRegistration`, `ListAsync` reads all `ClientRegistration` instances (clients are few), and `FindByRedirectUriAsync` filters that list. `CreateAsync`, `UpdateAsync` and `DeleteAsync` throw `ClientsChangeThroughCommands`. Secret validation compares the keyed digest. Precedent: the Chronicle kernel replaces the OpenIddict stores the same way (`Chronicle/Source/Kernel/Server/Authentication/OpenIddict/OpenIddictServiceCollectionExtensions.cs`, `ApplicationStore.cs`).
- **Scope store**: `Protocol/Stores/ResourceScopeStore : IOpenIddictScopeStore<ResourceScope>`, read-only over the `ResourceScope` read model; writes throw.
- **Authorization store**: stays operational (the OpenIddict MongoDB authorization store, registered explicitly). The facts are appended by `ApproveConsent` (S4) and `RevokeAuthorization` (S6). The record's status changes only on the S6 fence path.
- **Token store**: stays operational (the OpenIddict MongoDB token store with TTL), unchanged.
- No OpenIddict store reads a projection to decide whether a credential is usable. Clients and scopes are configuration, not credential status.
- Realm scope: Identity reads and writes the realm event store's **Default** namespace (a realm is an event store), pinned by section 7, not merely asserted.
- No entity caching: S3 calls `DisableEntityCaching()` on the OpenIddict core builder and Identity adds no cache of its own (section 6).

### Protocol-only commands

Arc has no internal-only command. `[ExcludeFromDiscovery]` only hides a command from the API description; the endpoint is still mapped (`Arc/Source/DotNET/Arc.Core/Commands/CommandEndpointMapper.cs`). The module therefore uses:

- `Protocol/ProtocolCommandAttribute.cs`: a marker, applied together with `[Cratis.Arc.ExcludeFromDiscovery]` (precedent: Studio's `CreatingModule`).
- `Protocol/ProtocolCommandFilter.cs : ICommandFilter`: rejects a `[ProtocolCommand]` with `CommandResult.Unauthorized` unless `ProtocolCommandScope.IsActive`, an `AsyncLocal` set only by `ProtocolCommands.Execute` (precedent: Studio's `StudioAuthorization` with `SystemCommandAttribute`).
- `Protocol/ProtocolCommands.cs`: runs `ICommandPipeline.Execute(command, serviceProvider)` inside the scope.
- The verified upstream proof is **not** command data. `Upstream/VerifiedUpstreamSignIn.cs` is a scoped `IVerifiedUpstreamSignIn` holding the `VerifiedAuthentication` and `ObservedProfile`, filled by `GitHubSignInEvents` from the validated OAuth ticket and injected into `Handle`, as Usher injects `ISignedInReporter`. An empty holder rejects the command.

### Host surface

- `IdentityHostApplicationBuilderExtensions.AddCratisIdentity<TBuilder>(this TBuilder, Action<IdentityOptions>? configure = null)` delegates to `services.AddCratisIdentity(...)`. Registrations use `TryAdd*`. It does **not** configure Arc or Chronicle: the host calls `AddCratisArc(…, arc => arc.WithChronicle(…))` next to it. Startup fails with `ChronicleNotConfigured` if `IEventStore` is missing.
- `MapCratisIdentity()` maps only the protocol endpoints and `/healthz`: no logout HTML, no consent HTML, no banner page. Commands and queries are mapped by `app.UseCratisArc()`. `app.UseCratisIdentity()` (after routing, before `UseCratisArc()`) opens the realm scope for Identity endpoints (section 7).
- `Identity.Host` is the reference host: Arc, Chronicle and Identity, its own consent and sign-out pages that call the commands and queries (S4), and dev seeding of clients and resources by **executing commands** (S3). Its `Program.cs` follows the embedding pattern of `WebApplicationWithUsher.cs`, and its `docker-compose.yml` runs a Chronicle kernel next to MongoDB.

## 6. Read-your-writes: bounded catch-up, defined failure, no stale caches

A command returns once its events are appended; `Account`, `UpstreamKeyLookup`, `ClientByClientId`, `ClientRegistration` and `ResourceScope` are updated later by projections. One retry plus `GetInstanceById` gives no catch-up guarantee: a first sign-in or a just-registered client can fail, and a rotation or retirement can appear late.

**Rule: an Identity command that changes protocol-visible state returns success only after the read models it feeds have handled its events, within a bound; otherwise it returns a defined failure.**

- `Protocol/ReadModelCatchUp.cs` (`IReadModelCatchUp`, internal) has two operations:
  1. **After an Identity command appended events.** Wait until every observer of the appended event types has handled the appended tail sequence number, using Chronicle's `AppendResult.WaitForCompletion(timeout)` (`Chronicle/Source/Clients/DotNET/Observation/AppendResultWaitForCompletionExtensions.cs`; the server waits per event-type tail and reports `TimedOut`, `FailedPartitions` and `OutstandingObservers`). The append result is captured from the scoped `IEventLog.AppendOperations` and filtered by the command's correlation id, as Arc does in `Arc/Source/DotNET/Chronicle/Commands/TransactionalCommandScope.cs`. The hook is `Protocol/IdentityCommandCatchUpScope : ICommandExecutionScope` (Arc discovers execution scopes in `CommandPipeline`), acting only on command types in `Cratis.Identity.*`; on timeout or a failed partition its `Complete` merges the defined failure into the result, as `TransactionalCommandScope.Complete` does. If Arc does not let a scope turn a successful result into a failure, the design changes: do not add a second pipeline.
  2. **After a `UniqueUpstreamKey` violation.** The loser of a concurrent first sign-in has no append of its own. Read `IEventLog.GetTailSequenceNumberForObserver(typeof(UpstreamKeyLookupProjection))` as the target, then poll `IProjections.GetStateFor<UpstreamKeyLookupProjection>()` (`ProjectionState.LastHandledEventSequenceNumber`) with backoff from 25 ms to 200 ms until it reaches the target, within the same bound. Then re-execute the command once. A second violation is the defined failure, never another retry.
- Bound: `IdentityOptions.ReadModelCatchUpTimeout`, default 5 seconds (Chronicle's default), validated greater than zero and at most 30 seconds.
- Defined failure: `Protocol/ReadModelsNotCaughtUp.cs`, an error in the `CommandResult` naming the outstanding read models. The events are committed and a retry is safe: sign-in then finds the key, and `RegisterClient` answers "already registered". Callers: sign-in returns the OAuth error `temporarily_unavailable` and issues **no** cookie or session, logged with `[LoggerMessage]`; client and resource commands return it to the operator as is; dev seeding retries until the host start timeout, then startup fails.
- In-process testing surfaces throw `CannotWaitForObserverCompletion`: unit specs replace `IReadModelCatchUp` through its interface; integration specs use the real kernel.
- **No stale caches.** OpenIddict entity caching is disabled (`DisableEntityCaching()`, S3), so each application and scope lookup reads the read model; Identity keeps no client, scope or account cache. Because `RotateClientSecret` and `RetireClient` return only after catch-up, the old secret or the retired client is refused on the first request after the command returns, on every replica.
- Specs. A paused projection is simulated by projection state that never advances. Unit: `Identity.Specs/Protocol/for_ReadModelCatchUp/when_the_projection_does_not_advance.cs` (`ReadModelsNotCaughtUp` within the bound, no second append), `when_the_projection_catches_up.cs`, `when_a_partition_failed.cs`; `Identity.Specs/Accounts/SigningIn/when_signing_in/and_the_read_models_lag.cs` (`temporarily_unavailable`, no session). Integration: `Identity.Integration.Specs/for_SigningIn/when_two_first_sign_ins_race.cs` (same upstream key, parallel, real kernel: exactly one account, both sign-ins succeed with the same subject, never a 500) and `for_ReadModelCatchUp/when_the_lookup_projection_is_held.cs` (defined failure, then a retry after release succeeds with the same subject). S3 adds the client cases.

## 7. Realm namespace pinning

Arc's Chronicle integration registers `TenantNamespaceResolver` (`Arc/Source/DotNET/Chronicle/ArcBuilderExtensions.cs`), which follows the ambient tenant (`Arc/Source/DotNET/Chronicle/Tenancy/TenantNamespaceResolver.cs`). The scoped `IEventStore`, and with it `IEventLog`, `IReadModels` and `IConstraints`, is built from that resolver once per DI scope (`Chronicle/Source/Clients/DotNET/ChronicleClientServiceCollectionExtensions.cs`). Asserting Default therefore does not pin it. The module pins it:

- `Realm/IdentityRealmScope.cs` (an internal `AsyncLocal` flag) and `Realm/IdentityRealmNamespaceResolver.cs : IEventStoreNamespaceResolver`. `AddCratisIdentity` replaces the `IEventStoreNamespaceResolver` singleton with this decorator around the host's resolver: inside the realm scope it returns `EventStoreNamespaceName.Default`; outside it delegates. Host work outside Identity is unchanged.
- Who opens the realm scope: (a) the `UseCratisIdentity()` middleware (after routing, before Arc) for Identity protocol endpoints and for Arc command and query endpoints whose type is in `Cratis.Identity.*`, from endpoint metadata, with the Arc route prefix of the Identity namespace computed at startup as fallback; (b) `ProtocolCommands` (sign-in, seeding); (c) the OpenIddict store adapters, `SignedInAccounts` and the `TokenEndpoint` reads, each in a child DI scope created inside the realm scope so their `IEventStore` is built for Default; (d) Identity reactors (outbox): verify that Chronicle hands a Default-namespace event to a Default-namespace scope, and open the scope there too if it does not.
- Fail closed: an Identity `ICommandFilter` and query filter reject any Identity command or query whose scoped `IEventStore.Namespace` (`Chronicle/Source/Clients/DotNET/IEventStore.cs`) is not Default, with `IdentityRealmNamespaceMismatch` (logged). An ordering mistake then refuses instead of writing a realm fact into a tenant namespace.
- Specs. Unit: `Identity.Specs/Realm/for_IdentityRealmNamespaceResolver/when_resolving_inside_the_realm_scope.cs` (Default while the inner resolver returns `tenant-a`) and `when_resolving_outside_the_realm_scope.cs` (delegates); `for_IdentityRealmNamespaceFilter/when_the_event_store_is_not_default.cs` (refused). Integration: `Identity.Integration.Specs/for_SigningIn/when_signing_in_with_a_non_default_ambient_tenant.cs`. The test host enables Arc tenancy and resolves tenant `tenant-a` for every request; sign-in, `Account.Mine` and a token request succeed; the account events are in Default and namespace `tenant-a` has none; a host-owned test command in the same host still lands in `tenant-a`.

## 8. Client and resource mutation authorization

The role is the issuer and security owner named in [Identity #20](https://github.com/Cratis/Identity/issues/20).

- `Hosting/IdentityPolicies.cs`: `public const string IssuerSecurityOwner = "Cratis.Identity.IssuerSecurityOwner";`.
- Deny by default: every client, resource and scope mutation command (`RegisterClient`, `ChangeClientRedirectUris`, `RotateClientSecret`, `RetireClient`, `RegisterResource`, `RegisterResourceScope`, `RetireResourceScope`) carries `[Authorize(Policy = IdentityPolicies.IssuerSecurityOwner)]`, and so do the configuration queries (`AllClients`, the resource listings). None of them is `[AllowAnonymous]` or relies on plain `[Authorize]`.
- The module defines the policy **name** only. The host provides the policy: Arc `services.AddArcAuthorizationPolicy<TPolicy>(IdentityPolicies.IssuerSecurityOwner)` (`Arc/Source/DotNET/Arc.Core/Authorization/AuthorizationPolicyServiceCollectionExtensions.cs`) or the ASP.NET Core policy of that name on the ASP.NET host. Arc already rejects an unknown policy before accepting traffic (`ArcAuthorizationPolicyRuntime`, `AuthorizationConfigurationValidator`); `AddCratisIdentity` adds an explicit startup refusal, `IssuerSecurityOwnerPolicyNotConfigured`, naming the policy.
- Dev seeding: `Identity.Host` runs the seed commands through `ProtocolCommands.Seed(...)`, which opens a `ProtocolCommandScope` of kind `Seeding` and executes in-process under a seeding principal that holds the issuer and security owner role. That principal is built only inside `ProtocolCommands`, never from a request, and the host's policy evaluates it normally: there is no bypass inside the policy. `Identity.Host` registers its own policy for its dev setup; production hosts register theirs.
- Denial specs come first in the step that adds the handlers, before any client or resource handler: `Identity.Specs/Clients/Registering/when_registering/and_the_caller_lacks_the_issuer_security_owner_role.cs` (refused, no event) and `and_the_caller_is_anonymous.cs`; the same pair for `RotateClientSecret`, `RetireClient` and `RegisterResourceScope`; `Identity.Specs/Hosting/for_AddCratisIdentity/when_the_issuer_security_owner_policy_is_missing.cs` (startup refuses); `Identity.Specs/Architecture/when_inspecting_configuration_mutations.cs` (every command and query under `Cratis.Identity.Clients` and `Cratis.Identity.Resources` requires that policy). Integration: `Identity.Integration.Specs/for_ClientRegistration/when_posting_register_client_without_the_role.cs` (403, no event) and `when_seeding_in_the_reference_host.cs` (succeeds through the seeding scope).

## 9. Corrections adopted in the outline

The [platform corrections](identity-platform-corrections-v1.md) (Proposed) change what this outline delivers in four places. Conflicts with signed-off specifications are open items in that note; this outline does not override them.

- **Federation (C1).** An upstream identity provider is an upstream of the realm. The realm issues the tenant- and resource-bound credential after tenant selection and a live membership check; a request header, route or tool argument selects a target and never constrains a credential. S1 and S2 sign-in and linking stay realm-side and never accept a raw external token as a tenant-bound credential.
- **Grant ownership (C2).** Identity writes only issuer grants: consent and authorization records, refresh families and revocation fences (S4, S6). Task authority, approvals, execution records and budgets belong to the product, and no slice in this outline writes them.
- **Consent and grant creation (C3).** The `Consent/Deciding` slice follows the commit protocol below.
- **Approved intent and execution (C4).** Identity has no execution record. The state `OutcomeUnknown` and its reconciliation belong to the product's approval flow.

### S4 acceptance: the grant activation barrier

The operational store is the single authority for the grant state machine `Pending`, `Decided`, `EvidenceRecorded`, `Active`, and every transition is a compare-and-set. `ApproveConsent` decides the request, a handler appends `ConsentGranted` and `AuthorizationGrantRecorded` idempotently (the proposed shared event source derived from `ConsentRequestId` is conditional on owner resolution of the stream-placement conflict recorded in the corrections note, section 6; it does not override identity-events-v1 §8 Account and Grant placements or §4.1 source-fact requirements), and only then does the operational authorization become usable and an authorization code get issued. Activation checks the revocation fence. A reconciler resumes a request stuck in `Decided` or `EvidenceRecorded`. A request that can no longer activate (expired, session ended, fence set) is closed by appending the withdrawal fact with a system cause and the reason `ActivationAbandoned`, so `MyConnectedApps` never lists a grant that was never usable. A deny appends nothing usable.

S4 is accepted only with these specs, under `Identity.Specs/Consent/Deciding/` unless marked integration:

- `when_approving/and_the_process_stops_after_the_decision.cs`: no code was issued, no authorization is usable, and the reconciler appends the evidence and then activates.
- `when_approving/and_the_process_stops_after_the_evidence_append.cs`: no code was issued, and the reconciler activates without a second append.
- `when_approving/and_the_process_stops_after_activation.cs`: the code is redeemable once, and a restart changes nothing.
- `when_approving/and_the_same_request_is_approved_twice.cs`: one grant, one event pair.
- `when_approving/and_the_approval_races_a_revocation.cs`: either the grant never becomes usable or it is fenced; never usable after the fence.
- `when_approving/and_the_request_expires_before_activation.cs`: `ActivationAbandoned` is appended and no connected app is shown.
- `when_denying.cs`: nothing usable is appended.
- `Identity.Integration.Specs/for_Consent/when_replaying_the_chronicle_facts.cs` (real kernel): replaying the facts enables no grant that the operational store did not activate.

## Deployments as of 2026-11-06

Read-only inventory of running or planned Identity deployments, taken on 2026-11-06 against Identity `origin/main` `2ecf09a` and Cratis/Infrastructure `origin/main` `ba566dd`. No Pulumi, Kubernetes or cloud CLI was used. It gates the entry condition in section 2: whether any deployment holds Identity accounts in MongoDB.

| # | Source | Command | Result |
|---|---|---|---|
| 1 | Infrastructure, whole tree at `origin/main`, Identity image or host names | `git grep -n -i -E "cratis/identity\|Identity[.]Host\|Cratis[.]Identity\|identity[.]cratis\|ghcr[.]io/cratis/identity" origin/main` (exit 1 = no match) | Not found |
| 2 | Infrastructure `Deployment/`, `Core/`, `scripts/`, `Documentation/`, `README.md`: the word "identity" | `git grep -n -i identity origin/main -- Documentation Core Deployment scripts README.md` | Not found as a service. Only prose about the per-app Kubernetes deploy account and AuthProxy tenant identity. No Identity workload |
| 3 | Pulumi project files and committed stack configuration | Files listed with `git ls-tree -r --name-only origin/main` and matched on "pulumi": `Core/Pulumi.yaml`, `Core/Pulumi.production.yaml`, `Core/Bootstrap/PulumiStateBackend.cs`; `Deployment/` holds only `Runners`. Both YAML files are covered by rows 1 and 2 | Not found (no Identity image or MongoDB connection in committed config). Stack state: see "Not checked" |
| 4 | All Cratis repositories, code search | `gh search code "Identity.Host" --owner Cratis --limit 30` | Found only in `Cratis/Identity` (project, `Docker/Dockerfile`, solution, README, integration specs). No compose, Kubernetes, Helm, Bicep or Terraform file elsewhere |
| 5 | same | `gh search code "Cratis.Identity" --owner Cratis --limit 40` | Found only in `Cratis/Identity`. The result list is capped at 40, so later hits were not seen |
| 6 | same | `gh search code "cratis/identity" --owner Cratis --limit 40` | Hits in `Cratis/Identity` (build comment in `Docker/Dockerfile`, document links) and unrelated substring matches (`/.cratis/identity-details/schema` in Arc repositories and Strategy). No deployment reference |
| 7 | same | `gh search code "ghcr.io/cratis/identity" --owner Cratis --limit 40` | Not found |
| 8 | GitHub container packages of the organization | `gh api "/orgs/Cratis/packages?package_type=container&per_page=100"` | No `identity` package among 36 (chronicle, studio-\*, stagehand\*, ante, direct\*, cratis-actions-runner, keldakraft\*, and others) |
| 9 | Docker Hub `cratis/*`, public API | `curl -s "https://hub.docker.com/v2/repositories/cratis/?page_size=100"`; `curl -s -i https://hub.docker.com/v2/repositories/cratis/identity/` | No `identity` repository among 23 (mongodb, base, base-mongodb, chronicle, cratis, chronicle-mcp, authproxy, prologue-\*, stage, stage-specrunner, prompter, planner, planner-worker, ai-agents\*, decision-engine, screenplay, showrunner-operator, montage). Direct lookup: HTTP 404 |
| 10 | Identity workflows | `git ls-tree -r --name-only origin/main -- .github/workflows`; `git grep -n -i -E "docker\|publish\|deploy\|ghcr\|nuget" origin/main -- .github/workflows` | `build.yml`, `openiddict-compat.yml`, `runner-guard.yml`, `verify-pull-request-policy.yml`. No deploy or image-publish step; `build.yml` packs NuGet as artifacts only |
| 11 | Deployable artifacts in the Identity repository | `git ls-tree -r --name-only origin/main`, matched on compose, Dockerfile, Helm, Bicep, Terraform, Kubernetes | `docker-compose.yml` (marked "Never deploy this file": Development, local MongoDB, ephemeral keys) and `Docker/Dockerfile` only |
| 12 | Planned deployment | `gh issue view 1 --repo Cratis/Identity` | Planned, not started. [#1](https://github.com/Cratis/Identity/issues/1) "Deploy Cratis Identity for Direct" is open with no comments: hostname, MongoDB database, persistent keys, GitHub OAuth app, Pulumi deployment alongside Direct. Related: Cratis/Direct#1354 (open) |

### Not checked

- Cloud resources (container apps, app services, any Kubernetes cluster): not checked (no access by instruction).
- Pulumi stack state and live stack configuration: not checked (no access by instruction). Only committed stack YAML was read.
- Private repositories outside the reach of the credentials used, and code the GitHub code index has not covered: unknown. Code search is capped and indexed.
- Developer machines and shared development environments, where someone may have run `docker-compose.yml` (its `identity-mongo` volume is local): not checkable from the sources above.
- Container registries other than GHCR and Docker Hub: not checked.

### Verdict for the S1 entry gate

No deployment with account documents was found, so S1 may delete the MongoDB account path without a migration. The evidence is absence in committed code, the code index, GHCR and Docker Hub: it shows that no deployment is declared in the places searched, not that none runs, and it says nothing about what a running instance's MongoDB holds. Identity has no deploy or publish workflow and no published image, so any instance would have been built from source or deployed by hand. The planned deployment in #1 has not started. The verdict is confirmed only when the owners answer the question on #1:

> Is there any shared or hosted Identity instance (dev, test, Direct, Studio, a pilot) that people have signed in to, and where does it run? If yes, may its `accounts` collection be deleted, or must the signed-in subjects be preserved so existing `sub` claims stay valid?

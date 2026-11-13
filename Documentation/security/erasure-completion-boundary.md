# Personal-data erasure: completion boundary

Identity has erasure events, a request command, a coordinator and a subject-key destruction stage, but **erasure is not enabled in production**. `Source/Identity/Accounts/Erasing/AccountErasureRequests.cs`, `AccountErasureCoordinator.cs`, `ChronicleAccountErasureLog.cs` and `DestroySubjectKeysStage.cs` implement those parts; no production registration supplies the workflow or its policies.

This page records where personal data lives, what the workflow can cover, and what remains **OPEN** or **BLOCKED** for [Cratis/Identity#51](https://github.com/Cratis/Identity/issues/51). Source paths identify implemented behavior; an acceptance spec is not approval of a production boundary. The normative rules stay in [identity-events-v1](../specs/identity-events-v1.md) §5 and §9 point 5; this page does not change them.

## 1. Where personal data lives

Legend: **PII** means the value type carries `[PII]`, so Chronicle crypto-shredding applies when the subject key is destroyed. **Not PII-marked** means the value is not protected by Chronicle encryption and needs separate handling.

| Store | Data | Marked | Evidence |
|---|---|---|---|
| Chronicle event store, Account stream | Email in `AccountEmailChanged` | PII | `Source/Identity.Contracts/Accounts/Email.cs:12`, `Events/AccountEmailChanged.cs:11` |
| Chronicle event store, Account stream | Personal name in `AccountNameChanged` | PII | `Source/Identity.Contracts/Accounts/PersonalName.cs:12`, `Events/AccountNameChanged.cs:11` |
| Chronicle event store, Account stream | Raw upstream subject in `UpstreamKeyLinkProposed` and `UpstreamKeySubjectRecorded` | PII | `Source/Identity.Contracts/Accounts/UpstreamSubject.cs:12`, `Events/UpstreamKeyLinkProposed.cs:14`, `Events/UpstreamKeySubjectRecorded.cs:14` |
| Chronicle event store, Account stream | Upstream display login in `UpstreamKeyDisplayIdentifierObserved` | PII | `Source/Identity.Contracts/Accounts/Login.cs:12`, `Events/UpstreamKeyDisplayIdentifierObserved.cs:14` |
| Chronicle event store, Account stream | Keyed lookup digest `KeyHash` in `UpstreamKeyLinkRecorded` | PII | `Source/Identity.Contracts/Accounts/UpstreamLookupDigest.cs:12`, `Events/UpstreamKeyLinkRecorded.cs:17` |
| Chronicle event store, public events | `AccountTombstoned` carries only `TombstonedAt` | No personal data by content | `Source/Identity.Contracts/Accounts/Events/AccountTombstoned.cs:11` |
| Chronicle event store, public events | `UpstreamKeyLinked` and `UpstreamKeyUnlinked` carry `KeyHash`, a keyed lookup pseudonym of the upstream subject | `UpstreamLookupDigest` is PII-marked in Identity; downstream protection, retention and erasure remain OPEN | `Source/Identity.Contracts/Accounts/UpstreamLookupDigest.cs`, `Events/UpstreamKeyLinked.cs`, `Events/UpstreamKeyUnlinked.cs` |
| Chronicle constraint index | `unique-upstream-key` over `KeyHash` | **Unverified**, see section 5 | `Source/Identity/Accounts/Constraints/UniqueUpstreamKey.cs:13-17` |
| Read model `Account` | `Name`, `Email`, `GitHubId`, `GitHubLogin` and personal fields in `LinkedUpstreamKeys` | Derived from PII events; stored-document erasure and replay are acceptance requirements, not an enabled cleanup stage | `Source/Identity/Accounts/Account.cs`, `AccountProjection.cs`; acceptance specs below |
| Read model `AccountLinkState` | `Email` and `Name`; reducer-state members `Proposals` (holding `UpstreamSubject`), `Logins` and `Links` (holding `KeyHash`) | Values derive from PII events; the read model itself is not separately marked | `Source/Identity/Accounts/Linking/AccountLinkState.cs:26-41`, `LinkProposal.cs:17` |
| Read model `UpstreamKeyLookup` | `LinkId` and an active flag per account | Pseudonymous ids only | `Source/Identity/Accounts/Lookup/UpstreamKeyLookupProjection.cs:14-23` |
| Query `LinkedAccountKeys` | `Login` display identifier, read from `AccountLinkState` | Derived | `Source/Identity/Accounts/LinkedAccountKeys.cs:23,50` |
| Legacy Mongo `accounts` collection | Former `github_id`, `github_login`, `name` documents | No account reader/writer remains (#109/#113); `LegacyAccountStoreProbe` only guards against a non-empty collection | `Source/Identity/Operational/LegacyAccountStoreProbe.cs`, `Source/Identity/Accounts/AccountReader.cs` |
| GitHub success log | Account reference only; GitHub id and login removed from both message and structured properties | This removes a new plaintext copy; existing log retention is OPEN | `Source/Identity/Upstream/GitHubSignInEventsLogging.cs`, `GitHubSignInEvents.cs`; `Source/Identity.Specs/for_GitHubSignInEvents/when_logging_a_successful_sign_in.cs` |
| Fence log, tombstones, outbox | Pseudonymous ids and sequences | Not personal data by design | identity-events-v1 §7; [storage note](../design/identity-package-and-storage-v1.md) §2 |
| Backups, exports, consumer stores, logs | Anything the stores above held at copy time | Outside the workflow | identity-events-v1 §5 |

The `AccountErased` and `AccountErasure*` events are catalogued in identity-events-v1 (§4 and §8). `DenyAllErasureAuthorization`, `NoErasureTenantScope` and `UnconfiguredErasureBoundaryPolicy` under `Source/Identity/Accounts/Erasing/` keep the workflow fail-closed.

## 2. Completion boundary

| Store | Erased | Remains | Not covered |
|---|---|---|---|
| Chronicle event log | Readable plaintext of PII-marked values, once the subject key is destroyed (identity-events-v1 §5) | Event envelopes, ids, timestamps and unreadable ciphertext | Copies made before destruction; command causation values, which key deletion does not erase; a restored key backup, which makes the ciphertext readable again |
| Chronicle constraint index | **Unverified** | **Unverified** | n/a |
| Read models | **BLOCKED:** no production deletion stage; stored values must be unreadable after erasure and remain so after replay (acceptance specs below) | Tombstones and pseudonymous references must not restore access | Existing plaintext documents, caches and backup copies need a deletion design; real-kernel behavior awaits CI |
| Fence log and tombstone | Nothing; kept on purpose | Pseudonymous account id, fence ids, sequences, `AccountTombstoned` | n/a |
| Legacy Mongo `accounts` collection | No current account reader/writer; the startup probe does not erase old documents | Any documents a deployment still holds; the guard refuses to start with them | Legacy copies and backups; their disposal policy remains OPEN |
| Application logs | New GitHub success messages omit upstream id and login | Previously written log entries | Log stores and their retention remain outside this workflow |
| Backups, exports, consumer stores | n/a | All copies | Retention boundary and acknowledgements are an open decision |

## 3. Implemented workflow and missing stages

`Source/Identity/Accounts/Erasing/ErasureStages.cs` defines this mandatory order. The coordinator refuses an unapproved boundary, missing scope or incomplete stage list. It reruns stages on retry, records real failures without repeating the same failure, and appends `AccountErasureRecorded` only after every stage succeeds for every scoped namespace (`AccountErasureCoordinator.cs`). Revocation comes first so a later failure must not restore access.

| Stage | Implemented | Spec-covered | Blocked on |
|---|---|---|---|
| `revoke` | No production erasure-stage implementation | Coordinator ordering and failure behavior only; integration harness uses `Satisfied` | Revocation-protocol wiring and proof that grants, families, sessions and PATs are denied |
| `destroy-subject-keys` | `DestroySubjectKeysStage`: `PII.DeleteEncryptionKeyFor` in every scoped namespace; never permits a replacement key | Real key destruction in `for_AccountErasure` integration specs; namespace failure specs | Approved namespace scope and deployed key-store purge/restore guarantees |
| `delete-read-models` | No production implementation | Empty-value replay specs and real-kernel read-model acceptance spec; harness uses `Satisfied` | Stored-document erasure/replay result and deletion/cache design |
| `purge-operational-store` | No production implementation; legacy account reader/writer removed | Coordinator stage contract only; harness uses `Satisfied` | Inventory and handling of remaining operational/legacy personal copies |
| `consumer-acknowledgements` | No production stage implementation | Coordinator and consumer-report contracts; harness uses `Satisfied` | Ante contract, consumer list and acknowledgement channel |

Spec paths: `Source/Identity.Specs/Accounts/for_AccountErasure/`, `Source/Identity.Integration.Specs/for_AccountErasure/given/ErasureHarness.cs`. `Satisfied` fakes do not prove cleanup by the four missing stages.

The read-model acceptance spec is `Source/Identity.Integration.Specs/for_AccountErasure/when_reading_read_models_after_erasure.cs`. It checks populated data first, then `Account` and `AccountLinkState` after real key destruction and after explicit projection/reducer replay, retaining tombstones and refusing an active account. **Real-kernel execution is deferred to CI; unreadability of stored read models is not established here.** If plaintext remains readable, a real deletion-stage design is needed; key destruction alone must not be reported as sufficient.

`Source/Identity.Specs/Accounts/for_AccountProjection/when_replaying_erased_personal_data.cs` and `for_AccountLinkStateReducer/when_replaying_erased_personal_data.cs` exercise Chronicle's released empty-string representation through the real projection/reducer scenarios. They retain the terminal tombstone even after an enablement fact. They do not prove stored-document deletion or constraint-index erasure.

## 4. Decided by agents (woksin delegation, 2026-11-08), einari to review post-hoc

These are agent-decided defaults pending einari's review, not approved product/legal policy. They stay unimplemented until confirmed; the fail-closed policies above remain unchanged. The following bullets record those proposals, not guarantees of the current workflow.

- **Backups and exports.** Crypto-shredding is the erasure mechanism. Destroying the person's key makes their encrypted data in backups and exports unreadable, and a restore never re-creates a destroyed key. Personal data stored outside crypto-shredding must move under it. Backup retention is a bounded window set by infrastructure policy, not a blocker.
- **Who may request.** The person themselves, after fresh authentication, or a realm admin acting on a verified request. A disabled or tombstoned account can still be erased.
- **When erasure is complete.** Key destroyed, erasure event appended, Identity read models purged, and each downstream product has acknowledged. The completion status is queryable and lists the products still pending.
- **Ante and other consumers.** A consumer reads the public Identity erasure event, erases or anonymizes its own data and acknowledges. Ante shows as pending in the completion status until its contract lands (Cratis/Ante#195).
- **Scope.** One realm per request. Erasing a person across realms takes one request per realm; person-level orchestration is a later item.

Still open:

- Whether the legacy `accounts` collection needs any erasure handling beyond the startup guard.
- Whether `AccountErased` may be recorded while retention-only stores still hold copies, beyond the bounded retention window above.

## Blocked on product/legal decisions

- **Backup, export and log retention; restore order:** approve retention boundaries and how erasure fences/key purges are reapplied before a restore serves traffic. The section 4 proposals are not evidence of deployed restore guarantees (identity-events-v1 §5).
- **Who may request:** decide person/admin/legal-request authority and authentication requirements; `Source/Identity/Accounts/Erasing/DenyAllErasureAuthorization.cs` currently allows nobody.
- **Ante and consumers:** settle [Cratis/Ante#195](https://github.com/Cratis/Ante/issues/195), the consumer list and acknowledgement channel. `UnlistedErasureConsumers.cs` lists nobody; `IErasureAcknowledgements.cs` defines the operational acknowledgement contract, not an approved consumer policy.
- **Namespace scope:** decide tenant/realm coverage per request; `NoErasureTenantScope.cs` returns no namespaces. Person-level orchestration across realms is later work in [Cratis/Identity#114](https://github.com/Cratis/Identity/issues/114), not supplied by this workflow.
- **Completion approval:** approve a `BoundaryVersion` only after coverage and duties are decided; `UnconfiguredErasureBoundaryPolicy.cs` supplies none. The coordinator cannot report completion without one.

All unqualified filenames above are under `Source/Identity/Accounts/Erasing/`. These decisions block enabling erasure, not the fail-closed scaffolding or its acceptance specs.

## 5. Unverified: constraint index and a `[PII]` digest

`unique-upstream-key` is declared on `UpstreamKeyLinkRecorded.KeyHash`, which is `[PII]` (`UniqueUpstreamKey.cs:13-17`, `UpstreamLookupDigest.cs:12`). The Chronicle sources are not part of this repository, so this review cannot confirm what the constraint index stores for an encrypted property (plaintext digest, ciphertext or nothing), nor whether the reservation survives key destruction. Until that is confirmed, the boundary neither claims the digest is erased from the index nor that it remains. If the index keeps a usable digest, that is a gap to raise with Chronicle.

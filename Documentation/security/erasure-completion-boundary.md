# Personal-data erasure: completion boundary (outline)

This outline records where Identity holds personal data today, what a completed erasure covers, and what stays outside it. It supports [Cratis/Identity#51](https://github.com/Cratis/Identity/issues/51). Statements with a cited source path are backed by that code. Everything labeled **PROPOSED** or **OPEN** is not implemented and not decided. The normative rules stay in [identity-events-v1](../specs/identity-events-v1.md) §5 and §9 point 5; this page does not change them.

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
| Chronicle event store, public events | `UpstreamKeyLinked` and `UpstreamKeyUnlinked` carry `KeyHash`, a keyed lookup pseudonym of the upstream subject | Pseudonymous, read by downstream consumers of public events; whether it is PII-marked or erasable downstream is OPEN (see section 4, consumer acknowledgements) | `Source/Identity.Contracts/Accounts/Events/UpstreamKeyLinked.cs:13,17` |
| Chronicle constraint index | `unique-upstream-key` over `KeyHash` | **Unverified**, see section 5 | `Source/Identity/Accounts/Constraints/UniqueUpstreamKey.cs:13-17` |
| Read model `AccountLinkState` | `Email` and `Name`; reducer-state members `Proposals` (holding `UpstreamSubject`), `Logins` and `Links` (holding `KeyHash`) | Values derive from PII events; the read model itself is not separately marked | `Source/Identity/Accounts/Linking/AccountLinkState.cs:26-41`, `LinkProposal.cs:17` |
| Read model `UpstreamKeyLookup` | `LinkId` and an active flag per account | Pseudonymous ids only | `Source/Identity/Accounts/Lookup/UpstreamKeyLookupProjection.cs:14-23` |
| Query `LinkedAccountKeys` | `Login` display identifier, read from `AccountLinkState` | Derived | `Source/Identity/Accounts/LinkedAccountKeys.cs:23,50` |
| Legacy Mongo `accounts` collection | Former `github_id`, `github_login`, `name` documents | No code reads or writes it; the account store is event-sourced. `LegacyAccountStoreProbe` only guards against a non-empty collection | `Source/Identity/Operational/LegacyAccountStoreProbe.cs:16` |
| Fence log, tombstones, outbox | Pseudonymous ids and sequences | Not personal data by design | identity-events-v1 §7; [storage note](../design/identity-package-and-storage-v1.md) §2 |
| Backups, exports, consumer stores, logs | Anything the stores above held at copy time | Outside the workflow | identity-events-v1 §5 |

The `AccountErased` and `AccountErasure*` events are catalogued in identity-events-v1 (§4 and §8). The request command, the coordinator and the key-destruction stage (`PII.DeleteEncryptionKeyFor` per namespace) exist under `Source/Identity/Accounts/Erasing/` with fail-closed defaults; nothing is registered in production yet.

## 2. Completion boundary

| Store | Erased | Remains | Not covered |
|---|---|---|---|
| Chronicle event log | Readable plaintext of PII-marked values, once the subject key is destroyed (identity-events-v1 §5) | Event envelopes, ids, timestamps and unreadable ciphertext | Copies made before destruction; command causation values, which key deletion does not erase; a restored key backup, which makes the ciphertext readable again |
| Chronicle constraint index | **Unverified** | **Unverified** | n/a |
| Read models | **PROPOSED:** rows removed or replaced; rebuilds must not restore plaintext | Pseudonymous `LinkId`s | Copies in backups |
| Fence log and tombstone | Nothing; kept on purpose | Pseudonymous account id, fence ids, sequences, `AccountTombstoned` | n/a |
| Legacy Mongo `accounts` collection | Nothing implemented here; account erasure goes through the `AccountErased` event workflow (section 3), which is PROPOSED | Any documents a deployment still holds; the guard refuses to start with them rather than erasing them | Logs: `GitHubSignInEventsLogging` writes the GitHub id and login to application logs, and log stores are outside the boundary; backups |
| Backups, exports, consumer stores | n/a | All copies | Retention boundary and acknowledgements are an open decision |

## 3. Proposed workflow (PROPOSED, not implemented)

Order follows identity-events-v1 §5. Revoking first keeps access denied even if a later stage fails.

1. Revoke grants, families, sessions and personal access tokens through the revocation protocol.
2. Purge operational personal data.
3. Destroy the subject's encryption keys in every relevant namespace (`IEventStore.PII.DeleteEncryptionKeyFor`).
4. Remove or rebuild read models and caches so no plaintext survives a replay.
5. Append `AccountErased`.

The fence log and the tombstone are kept so a replay or restore cannot re-enable access. Stage progress would be operational state; events would mark only the request, a real failure and completion.

## 4. Decided by agents (woksin delegation, 2026-11-08), einari to review post-hoc

These decisions were made without product or legal sign-off and are revisable.

- **Backups and exports.** Crypto-shredding is the erasure mechanism. Destroying the person's key makes their encrypted data in backups and exports unreadable, and a restore never re-creates a destroyed key. Personal data stored outside crypto-shredding must move under it. Backup retention is a bounded window set by infrastructure policy, not a blocker.
- **Who may request.** The person themselves, after fresh authentication, or a realm admin acting on a verified request. A disabled or tombstoned account can still be erased.
- **When erasure is complete.** Key destroyed, erasure event appended, Identity read models purged, and each downstream product has acknowledged. The completion status is queryable and lists the products still pending.
- **Ante and other consumers.** A consumer reads the public Identity erasure event, erases or anonymizes its own data and acknowledges. Ante shows as pending in the completion status until its contract lands (Cratis/Ante#195).
- **Scope.** One realm per request. Erasing a person across realms takes one request per realm; person-level orchestration is a later item.

Still open:

- Whether the legacy `accounts` collection needs any erasure handling beyond the startup guard.
- Whether `AccountErased` may be recorded while retention-only stores still hold copies, beyond the bounded retention window above.

## 5. Unverified: constraint index and a `[PII]` digest

`unique-upstream-key` is declared on `UpstreamKeyLinkRecorded.KeyHash`, which is `[PII]` (`UniqueUpstreamKey.cs:13-17`, `UpstreamLookupDigest.cs:12`). The Chronicle sources are not part of this repository, so this review cannot confirm what the constraint index stores for an encrypted property (plaintext digest, ciphertext or nothing), nor whether the reservation survives key destruction. Until that is confirmed, the boundary neither claims the digest is erased from the index nor that it remains. If the index keeps a usable digest, that is a gap to raise with Chronicle.

# Recovery: independent fence log and restore

Status: Draft. Owner: Identity module maintainer with the infrastructure owner of the realm host. This runbook is documentation of the procedure; it is **not** evidence that independent custody is provisioned in production or that a restore drill has completed. Provisioning belongs to [Cratis/Infrastructure](https://github.com/Cratis/Infrastructure); the rehearsal is tracked in [#299](https://github.com/Cratis/Infrastructure/issues/299). Related Identity work: [#42](https://github.com/Cratis/Identity/issues/42) (revocation recovery) and [#18](https://github.com/Cratis/Identity/issues/18) (paused).

The independent fence log is the one store that must survive the restore of both consumers, the operational grant store and Chronicle. Without it a restored grant store can re-admit a refresh token that was revoked after the backup was taken. Read [revocation §§3 and 6–8](../../specs/identity-revocation-v1.md) and the [shared recovery rules](../compromise-matrix-v1.md#recovery-rules-shared-by-the-runbooks) first.

## What ships and what is operator work

| Item | Owner |
| --- | --- |
| Contract: `Cratis.Identity.FenceLog` (ports `IIndependentFenceLog`, `IReservationLog`, `IRecoveryLog`, records, `FenceLogOptions`) | this repository |
| MongoDB adapter: `Cratis.Identity.FenceLog.Mongo` (`MongoIndependentFenceLog`, `IndependentFenceLogConnection`) | this repository |
| Independent replica set, workload permissions, backup exclusion, unique `sequence` index | infrastructure owner |
| `fence-custody` metadata document for a new realm, recorded `InstanceId` and `MinimumSequence` | operator, from independent evidence |
| `RestoreId`, `RestoreKind`, `RestrictionsReestablished` declaration | operator, per restore |

The log must run on a replica set with a different replica-set identity than the operational store. The host reads the identity with the `replSetGetConfig` command and refuses equal or unreadable identities. A second database, an alias or different connection text on the same deployment does not count as independent. The log is excluded from the operational and the Chronicle backup sets.

## Configuration

Bind `Identity:FenceLog` with the connection string injected from the approved secret mechanism. `Database`, `InstanceId` and `MinimumSequence` are non-secret and come from independent records. Never lower `MinimumSequence` to make startup pass. A host never initializes an empty log automatically.

## Choosing the restore kind

| What was restored | `RestoreKind` | Replay before readiness |
| --- | --- | --- |
| Operational grant store only | `Operational` | independent denials and cancellation markers replayed into the store; Chronicle delivery is not awaited |
| Chronicle only | `Chronicle` | delivery markers rebuilt and keyed replay to the watermark completed |
| Both | `Both` | both of the above |

Chronicle-only rollback needs an explicit declaration; it cannot be inferred from operational watermarks.

## Procedure

1. Quiesce every affected replica and use route. Preserve the independent log and its backups untouched.
2. Restore the consumer or consumers from backup.
3. Give one recovery owner a fresh, opaque `RestoreId` and the `RestoreKind`. Other replicas run without the declaration and wait. A changed kind, a completed declaration or a declaration reused by another host is refused. After an interrupted owner, use a new declaration.
4. Start. Every start begins unready. The owner reconciles abandoned reservations, validates the whole log, replays denials, advances the independent recovery epoch and publishes the epoch facts. Pre-restore refresh families are rejected, not upgraded.
5. Set `RestrictionsReestablished: true` only in a fresh declaration, and only after the accountable owners have independently reconstructed every restriction the log does not cover and invalidated every pre-epoch stateful capability. Passing grant replay is not enough.
6. After completion remove the declaration from normal starts. Never clear a gate or reset an epoch directly.

Do not create the `fence-custody` marker to repair a lost or re-created log, and never copy it from a consumer backup. A lost log needs independent recovery of the original complete log plus external continuity proof; an empty replacement is refused.

## Chronicle workload credentials

The Chronicle connection of a hosted realm authenticates a workload. It must be issued by an authority that is not the people realm: not signed by, derived from or stored with the realm's signing keys, OpenIddict client secrets or upstream provider secrets, and held at its own secret-store path. Otherwise a realm compromise also yields the credential that rewrites realm events. The repository cannot prove who issued a credential; infrastructure provisioning does. See the [shared host runbook](shared-identity-host.md#chronicle-workload-credentials).

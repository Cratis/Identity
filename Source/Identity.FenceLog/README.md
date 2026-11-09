# Cratis.Identity.FenceLog

Cratis.Identity.FenceLog is the storage-independent contract of the independent fence log used by Cratis Identity, for anyone implementing or operating a log that must survive the restore of the grant store and Chronicle.

The package holds the ports `IIndependentFenceLog`, `IReservationLog` and `IRecoveryLog`, the immutable records they exchange (`GrantFence`, `FenceRecord`, `OutboxEntry`, `FenceReservation`, `RecoveryCustody`, `RecoveryCheckpoint`, `RecoveryEpochIntent`), the closed enums and `FenceLogOptions`, bound from `Identity:FenceLog`. It references no storage driver. Persisted JSON shapes are a compatibility contract: do not rename properties.

`Cratis.Identity.FenceLog.Mongo` is the MongoDB implementation. The operator procedure, including what is infrastructure work, is in the [fence-log restore runbook](https://github.com/Cratis/Identity/blob/main/Documentation/security/runbooks/fence-log-restore.md).

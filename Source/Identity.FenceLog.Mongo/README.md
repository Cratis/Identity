# Cratis.Identity.FenceLog.Mongo

Cratis.Identity.FenceLog.Mongo is the MongoDB adapter of the Cratis Identity independent fence log, for hosts that run the log on a separately provisioned replica set.

`MongoIndependentFenceLog` implements `IRecoveryLog` with majority read and write concern. `IndependentFenceLogConnection` refuses a deployment that shares a server and database with the operational store, and `Verify` proves the replica-set identities differ and that custody continuity holds. The adapter never creates its custody metadata marker, and requires a replica set with the `replSetGetConfig` probe permitted. Independent provisioning and backup exclusion are operator work; see the [fence-log restore runbook](https://github.com/Cratis/Identity/blob/main/Documentation/security/runbooks/fence-log-restore.md).

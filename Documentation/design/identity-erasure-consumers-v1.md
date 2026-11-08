# Identity erasure: consumer obligations (draft v1)

Status: **Draft, pending decisions in [Identity #51](https://github.com/Cratis/Identity/issues/51).** This note does not approve the erasure completion boundary and does not define a `BoundaryVersion`. It records what Identity implements for consumer follow-up and what it asks of consumers. See [events §5](../specs/identity-events-v1.md) for the normative erasure rules.

## What Identity does

After an account's keys are destroyed, `AccountErased` is published. Consumers of public account events (Studio, Direct, Ante and any other approved consumer) hold copies Identity cannot reach, so they must delete their own copies of that account's data.

Acknowledgements are an **operational fact, not an event**. Identity records, per opaque `ErasureId`, which consumer reported its cleanup and when. The record carries no personal data. Identity reports the state per erasure:

- **Acknowledged**: listed consumers that confirmed deletion.
- **Pending**: listed consumers that owe a confirmation that has not arrived.
- **Retention only**: listed consumers documented as retaining data; no confirmation is expected.

Acknowledgements are informational. **Nothing in the erasure workflow waits on a consumer**: a missing or late consumer never blocks or fails an erasure, and `AccountErased` is not delayed for it. Acknowledgements from unlisted consumers are ignored.

## What each consumer must do

| Consumer | Obligation on `AccountErased` | Acknowledgement | Status |
|---|---|---|---|
| Studio | Delete or irreversibly anonymize every copy keyed by the account subject, `LinkId` or `KeyHash`, including read models, caches and logs it controls. | Report the `ErasureId`. | Duty not yet confirmed |
| Direct | Same as Studio. | Report the `ErasureId`. | Duty not yet confirmed |
| Ante | Unknown: no Ante erasure contract exists in this repository. | Not defined. | **Open**: Ante owner must say whether it deletes and acknowledges, or is listed as retention only |

Consumers must treat `AccountErased` idempotently and must not re-create data for an erased subject from a later replay.

## Not decided here

- The consumer list and each consumer's duty. The default listing is empty (`UnlistedErasureConsumers`); nothing is invented.
- How acknowledgements reach Identity (endpoint, event or other channel) and the operational store that holds them. Only the contract and the report exist.
- Whether `AccountErased` must wait for acknowledgements. The current design says it does not.
- The approved boundary and its `BoundaryVersion`; backup and export retention windows.

Erasure stays fail-closed: the authorization, tenant scope and boundary policies default to refusing, and nothing registers an enabling policy in production.

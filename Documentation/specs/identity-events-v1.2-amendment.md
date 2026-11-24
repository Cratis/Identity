# Identity events v1.2 amendment: default event type ids

```text
Spec id: identity-events
Version: 1.2
Status: Draft pending owner confirmation
Issue: https://github.com/Cratis/Identity/issues/149
Sign-off issue: https://github.com/Cratis/Identity/issues/14
Supersedes: identity-events v1 §2.3 (stable event type ids), the id column of the §8 catalogue table, the "Source fact type id" column of the §4.1 source-fact table and its prose, §9 point 1, and the "contract type id" example in §4.1 point 3
```

## 1. Status and scope

This amendment is a **Draft**. Following the [conventions](README.md#conventions), the signed-off [identity-events v1](identity-events-v1.md) changes only through a new version file; v1 stays readable and unchanged apart from a pointer line. Everything in v1 other than the passages listed under `Supersedes` stays in force: names, streams, generations, PII, source facts, forbidden content and the §8 columns other than the id.

## 2. The rule: the default id is the event type id

1. Every Identity event is declared with a plain `[EventType]`. Its event type id is Chronicle's default, the short CLR type name (`Type.Name`: no namespace, case-sensitive). Public events follow the same rule.
2. The name MUST be unique across the event store. The contract checker rejects two distinct current types that resolve to the same name; a prior generation (`[EventTypeGenerationFor]`) shares its current type's name by design.
3. A persisted event type name MUST NOT be renamed. Moving the type between namespaces or folders is safe; the id does not contain them.
4. Exception: an explicit id string is allowed **only** to keep an id that is already persisted after a CLR rename, or for a documented retained contract. Such an event MUST have a catalogue row with `retainedExplicitId: true` and a non-empty `reason`, and the attribute id MUST equal the row's `typeId`. There is no exception today.
5. The catalogue keeps its `typeId` column; for an allocated row it equals the event's `name`. A placeholder row keeps `typeId: null`.

## 3. Contract checker

- The id is resolved the way Chronicle resolves it: the attribute id when non-empty, otherwise the short CLR type name (for a generation record, the name of the referenced current type). Ids are compared with ordinal, case-sensitive equality; a UUID is no longer required.
- `duplicate_event_type_name` rejects distinct current types with the same resolved id. `explicit_event_type_id_not_allowed` rejects an explicit id without the catalogue exception of rule 4.
- Released-schema gate: a released row whose `typeId` is a UUID matches the current row with the same assembly, namespace, name and generation whose `typeId` equals that name, and a released fixture path `fixtures/<uuid>/<generation>.json` matches `fixtures/<Name>/<generation>.json`. All other row content and the fixture bytes MUST be identical. The translation never applies to a released row that is already name-based, and its destination is always the released row's own unchanged name, so it fires once and cannot rewrite history to another identity.

## 4. Outbox and fence log

The restriction outbox deduplication pair is `(FactId, event type id)`, written `"{FactId:D}:{event type name}"`, and the recovery-epoch key is `"{FenceId:D}:RealmRecoveryEpochAdvanceRecorded"`. Restriction `FactId`s are derived deterministically from the fence id and the event type name, so their values changed with the id; the reuse `DetectionId` is still derived from the fence id and `reuse-detection`, unchanged, and is null on every non-reuse entry. Grant-state validation recomputes them and rejects any persisted entry whose identity is not the canonical derivation, including the former UUID-suffixed shape. There is **no migration**: no environment is known to hold Identity events, fence-log or outbox rows written under the previous ids (data check recorded on #149). Were such data found, a dated two-shape reader would be needed and the user would decide.

## 5. Legacy ids (migration record)

The v1 §8 and §4.1 tables, and the vectors written before this amendment, list the previous pinned ids. Each maps to its default id:

| Event type id (default) | v1 pinned id |
|---|---|
| AccountDisabled | 8e1e68fd-e4fb-456a-9af4-f95185be950b |
| AccountDisablementRecorded | e26026d0-d445-4647-ba9f-f7f1d868c8ac |
| AccountEmailChanged | 92fde774-1586-4d45-a354-8349a9f5e3fd |
| AccountEnabled | 4fa5a4be-c0e1-4ad1-81d4-4eb304aab74d |
| AccountEnablementRecorded | 46c54931-10f5-4069-9c22-b059008b3fc1 |
| AccountErased | a87a0deb-fa35-4ae3-bb91-1b1db8f1b5fe |
| AccountErasureFailed | af2d3634-9adc-48eb-b00d-6565df2a38e8 |
| AccountErasureRecorded | 213861f1-a227-4f67-acae-a0619f4eb946 |
| AccountErasureRequested | 6959b7dd-2e06-4d26-9c28-f92c234ef92f |
| AccountNameChanged | 2eba996c-8d19-4d5c-9648-3dca02daf879 |
| AccountRegistered | fd1cb0b5-17c1-4f34-b625-c318dc928d97 |
| AccountRegistrationRecorded | c2c8ac1b-6110-433f-93d6-1e9222852baa |
| AccountTombstoneRecorded | f67aa184-1052-4a3e-863a-13059de335e0 |
| AccountTombstoned | 6d6b4f99-5ac3-4493-b892-13a5e44c6518 |
| UpstreamKeyDisplayIdentifierObserved | ecb91993-88b6-42a1-bdbc-ee644bd50fca |
| UpstreamKeyLinkCleared | 0f4ff8c1-a7e4-48e4-9bd1-65abbe2b465d |
| UpstreamKeyLinkProposed | b49850a5-1179-489b-bd37-96a4bb25ff69 |
| UpstreamKeyLinkQuarantined | a407f6de-7435-47b3-bcc8-ae3498010e71 |
| UpstreamKeyLinkRecorded | 0948769e-d249-472e-abb3-93e8743379da |
| UpstreamKeyLinkRejected | 81cf15b3-1e52-441d-b55c-0b89853f8861 |
| UpstreamKeyLinked | e2d1b4ad-914e-41c6-9187-c589fb36d68c |
| UpstreamKeySubjectRecorded | b0f49e52-c457-484b-adf8-8a2207a36869 |
| UpstreamKeyUnlinkRecorded | de708442-c9b3-4d8b-b677-01450ef9248e |
| UpstreamKeyUnlinked | f84b03e3-e0c1-4593-962e-917c56e50bcc |
| ClientAdded | f381fb17-b22e-4393-9cee-e9b7ea8150c6 |
| ClientPermanentlyRetired | 6eb83139-5fbe-43a4-813e-19b0f4e70d3c |
| ClientRedirectUrisChanged | 1dfa8ff9-2538-44c2-8628-00dc084fcabe |
| ClientRegistered | 0e4bb9ea-86c7-4000-bbfd-308a2dbf4989 |
| ClientRetired | 8fa4f300-a66f-442d-ad15-37dc3bdd692f |
| ClientSecretRotated | 4a913d36-056b-4e71-81ca-c90b207393b3 |
| RedirectUrisChangedForClient | 9aba05c4-2560-4ea5-aba4-5037b41b017f |
| SecretRotatedForClient | 851b6888-97b1-4954-8855-11395793cfce |
| ResourceRegistered | e57de89b-215d-43ab-a235-83a5e517934a |
| ResourceRegistrationRecorded | 9737db97-da6b-484e-afe6-760188549102 |
| ResourceScopeRegistered | 24b65e88-8dbb-4e02-8f37-9d591eb29d20 |
| ResourceScopeRegistrationRecorded | c1635433-c34b-4b8f-b551-f20fd541fbde |
| ResourceScopeRetired | 6a6869c9-423b-4746-8aa5-93b989b6895f |
| ResourceScopeRetirementRecorded | 3a28650d-61a9-4d9f-bb06-4fdebb205803 |
| AuthorizationRevocationRecorded | ac88f9a7-6364-4f20-9301-d533b491b641 |
| FenceLogCustodyEstablished | b90e65de-fd11-43c9-b032-e6f73d5ca089 |
| FencePublicationCheckpoint | 507bb271-f4cf-43ca-a5a8-3e3a80fd0093 |
| RealmRecoveryEpochAdvanceRecorded | bf2cbb34-f181-4931-be63-52d68a143be4 |
| RefreshTokenFamilyRevocationRecorded | a80e52ab-051e-4771-bff2-488dccd8aa44 |
| RefreshTokenFamilyStarted | d9a448be-ccf1-4ab5-b43c-f17dad0ba6c3 |
| RefreshTokenReuseDetected | bedbc6d5-2870-4cf7-aa5e-79e7a2bf3cb6 |

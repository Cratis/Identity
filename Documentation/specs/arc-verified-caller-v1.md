# Verified caller and credential matrix

```text
Spec id: arc-verified-caller
Version: 1
Status: Draft
Issue: https://github.com/Cratis/Arc/issues/3701
Sign-off issue: https://github.com/Cratis/Identity/issues/14
Supersedes: None
```

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Purpose

Verification paths produce one caller shape without confusing human, delegated, and machine authority. The caller supplies verified inputs to authorization; it is not authorization by itself. This is a draft logical record, not a shipped public API.

## Terms

Person, Account, Realm, Issuer, Subject, Linked key, Resource, Resource indicator, Audience, Tenant, Organisation, Environment, Actor, Credential kind, Assurance, Policy epoch, Access token, ID token, Refresh token, and PAT have exactly the meanings in the Terms table of [identity-contract-v1.md](identity-contract-v1.md), Cratis/Identity#24. Shared meanings are not redefined here.

A **verified caller** is a request-local record emitted after the selected verifier succeeds. A **service-principal allowlist** maps a verified machine identity to explicit operation permissions and tenant rules. A **per-work grant** is a checked server-side work record, not a credential. A **principal kind** distinguishes person, delegated actor, organisation automation, cluster workload, and runner; it is not a credential classification.

## Normative contract

MUST, MUST NOT, and SHOULD use [RFC 2119](https://www.rfc-editor.org/rfc/rfc2119) meanings. Requirements apply after sign-off.

### Caller record version 1

The logical field mappings below extend identity-contract-v1 §6 and remain subject to its required Direct/Studio consumer review. Camel-case names are proposed Arc record fields, not new token claims or a new signed-envelope serialization.

| Field | Requirement |
| --- | --- |
| `contractVersion` | MUST be a string identifying the verified signed-context version or supported local verification profile, never caller input. This local profile uses `"1"`. Unknown profiles MUST be refused. |
| `principalKind` | MUST identify `person`, `delegated-actor`, `organization-automation`, `cluster-workload`, or `runner`. These proposed enum spellings do not rename the shared Organisation term. |
| `accountKey` | MUST contain exact `issuer` and lowercase UUID v4 `subject` for a person, as defined in identity-contract-v1; MUST be absent for service-only calls. |
| `person` | MUST be the product person reference resolved from confirmed account-key mapping for person/delegated calls; MUST be absent for service-only calls. It is not a replacement wire `sub`. |
| `actor` | MUST represent the current registered `iss`/`sub` from verified `act` for delegated/on-behalf calls. It MUST NOT be invented from public client ids or PAT labels. Historical nested actors MUST NOT authorize. |
| `servicePrincipal` | MUST identify the verified service identity for machine-only calls; MUST NOT be promoted to an Actor acting for a person or to a person UUID. This field extends the logical record for machine permissions. |
| `credentialKind`, `credentialId` | MUST reflect the verified record: Browser session, OAuth grant, PAT, on-behalf session, machine principal, or workload, as defined in identity-contract-v1. Exact enum serialization remains open. The id MUST be non-secret. A token `jti` alone MUST NOT be treated as a grant id. |
| `grantId` | MUST identify the checked delegation or per-work record where applicable and MUST come only from the verified credential or a server-side mapping keyed by verified `credentialId`, never request headers, body, or query. It MUST NOT substitute for presenter authentication. Its token-claim mapping is not defined by this record. |
| `client` | MUST use `{ id, authentication }` where a client is present. `authentication` MUST distinguish `declared` from `authenticated`. Public client ids and PAT labels MUST NOT establish authenticated actor authority. |
| `resource`, `environment` | MUST be registered references established by verified credential constraints and trusted installation binding, not request labels. |
| `tenant` | MUST be the verified product tenant for workspace operations, corresponding to token `tid` where applicable. It MUST match the independently resolved target and current membership. Machine work MUST obtain its tenant from a checked per-work binding. |
| `organisation` | MUST come only from a confirmed Access tenant binding, if relevant. It MUST NOT substitute for tenant membership. |
| `scopes` | MUST preserve the verified ceiling and be intersected with live roles and policy. Empty scopes MUST NOT mean unrestricted person access. |
| `permissions` | MUST preserve explicit service-principal operation permissions where applicable; MUST NOT confer person roles. |
| `assurance` | MUST retain `idp`, integer NumericDate `auth_time`, `acr` (`basic` if unknown), and optional mapped `amr` for person calls. A workload MUST NOT fabricate human assurance. |
| `senderBinding` | MUST contain either no binding or the verified DPoP result (`jkt`, accepted proof `jti`). Raw proofs MUST NOT be forwarded as evidence. A token with `cnf.jkt` MUST NOT be accepted as a plain bearer. Missing DPoP header, wrong proof key thumbprint, or replayed proof `jti` MUST cause 401 and MUST NOT produce a caller. DPoP optionality applies only to unbound tokens. |
| `policyEpoch` | MUST be a nonnegative integer when organisation policy applies. A stale epoch MUST force current-policy re-evaluation; it MUST NOT independently authorize. An unavailable required policy check MUST cause refusal. |

Arc MUST construct exactly one caller for UI, REST, and MCP from a verified AuthProxy envelope, locally verified PAT after dispatch, native OAuth where enabled, or allowlisted verified workload. Unsigned identity headers MUST NOT authenticate a caller. Authorization MUST intersect product policy, scopes or service permissions, tenant, environment, grant bounds, assurance requirements, and current fences.

A request with more than one credential, or a credential in the URI query, MUST be refused before caller construction (RFC 6750 §2 and OAuth 2.1). The verifier MUST NOT select one of multiple credentials silently.

For delegated calls, the grant record's `accountKey`, current `actor` (`iss`, `sub`), `resource` and catalog audience, `tenant`, and `environment` MUST equal the verified token's corresponding values and trusted installation binding. A mismatch MUST cause refusal. A request-supplied grant reference MUST be ignored or refused, never used to select a record.

### Credential matrix

| Principal kind | Issuer | Audience | Authentication | Authorization owner | Tenant binding | Permitted exchanges |
| --- | --- | --- | --- | --- | --- | --- |
| Person | Realm Identity for OAuth; resource-local PAT issuance | One catalog audience, or fixed PAT resource | OAuth with PKCE; DPoP where bound; application PAT verification; verified browser/on-behalf context where applicable | Product membership and roles intersected with scopes | Fixed per credential | Delegation within consented grant; PAT exchange is not enabled by this draft |
| Delegated actor | Realm Identity | One allowed destination audience | Authenticated agent and DPoP-bound exchanged token | Product intersected with server-side lineage | Inherited, no wider | Only named actors, destinations, depth, budgets |
| Organisation automation | Realm Identity | One catalog audience | Confidential client credentials, `private_key_jwt`, DPoP | Product service-principal rules, never person roles | Fixed at registration | No exchange into person authority |
| Cluster workload | Cluster issuer or workload CA | Receiving app, for example `work-api`; never API-server default | TokenReview with live bound pod, or workload mTLS | Receiving app allowlist and explicit permissions | Checked per-work grant for tenant work | None through the people authority |
| Runner | Registering product | Runner endpoints only | Registered key proved per session | Registering product and checked per-work grant | Per-work grant | None |

The DPoP requirements for authenticated delegated and organisation-owned agents do not impose DPoP on every MCP client. [MCP client DPoP support is optional](arc-mcp-authorization-v1.md); explicitly bound tokens still require their proofs.

Endpoints MUST declare accepted principal kinds. A workload MUST be refused on a person-only endpoint and a person credential on a workload-only endpoint. Workload credentials MUST NOT be exchanged through Identity for person or realm tokens. A valid resource audience MUST NOT be treated as a service-principal allowlist entry.

### Workload verification and cache

For service-account tokens the handler MUST validate cluster issuer, receiving-app audience, expiry, and binding to a live pod. TokenReview MUST specify the receiving app's expected audience and its returned `audiences` MUST include it. The API-server default audience MUST NOT satisfy this check. Verified cluster issuer, namespace, service account, bound pod name, and bound pod UID MUST map through the configured allowlist to explicit permissions and tenant rules. Unknown identities MUST be refused, including otherwise-valid customer workloads at platform endpoints.

Projected service-account token lifetime SHOULD be short; ten minutes is proposed, not locked. For mTLS the verifier MUST check the workload-CA chain, SAN identity, and certificate validity; short lifetime MUST bound revocation exposure. The verified mTLS service identity MUST also pass the allowlist.

The **deleted-pod rejection latency** `L` is a proposed maximum of 30 seconds. A successful TokenReview cache entry MUST expire no later than `min(token expiry, validation time + L)`. Earlier deletion invalidation MUST still meet `L`. After cache expiry, unavailable revalidation MUST cause refusal. Revalidation MUST re-establish pod liveness. Cache keys MUST bind token and expected audience; results MUST NOT be reused across audiences.

An endpoint performing work for a person MUST authenticate the service principal and separately check the per-work grant. Authorization MUST intersect both permission sets. A grant for another service principal MUST be refused. Tenant and person provenance MUST be explicit in background work, not inferred from ambient state. Any represented-person authority MUST remain in the checked per-work record rather than converting a workload into a person credential.

## Test vectors

Fixtures use resource `https://work.example.test/api`, audience `work-api`, tenant `tenant-fixture`, and registered environment `test`. The synthetic person account key uses issuer `https://login.example.test` and subject `8b167ad2-0f89-4c78-9d42-6d4580495d32`.

| Input | Expected result |
| --- | --- |
| Valid person PAT and permitted operation | `principalKind=person`, Credential kind PAT; account key and product person retained, live policy checked. |
| Valid delegated token with person UUID `sub`, current `act` object, active lineage | `principalKind=delegated-actor`; product person and current Actor distinct. |
| Valid organisation client-credentials token | `principalKind=organization-automation`, Credential kind machine principal; no person or fabricated Actor. |
| Bound live pod token and allowlisted service account | `principalKind=cluster-workload`, Credential kind workload; explicit permissions, no person. |
| Runner key proof and matching per-work grant | `principalKind=runner`; only granted runner work; no person credential. |
| Correct audience, non-allowlisted service account, including customer workload | Refused. |
| TokenReview `authenticated=true` but no app audience, or only API-server audience | Refused. |
| Valid mTLS chain but SAN not allowlisted | Refused. |
| Grant for `service-a` presented by authenticated `service-b` | Refused. |
| Validate at `t=0`, pod deleted at `t=1`, token expiry `t=600`, request `t=31` | Refused; no cached success after `t=30`, deletion rejected within 30 seconds. |
| Validate at `t=0`, token expiry `t=10`, request `t=11` | Refused despite 30-second maximum. |
| Revalidation unavailable after cache expiry | Refused; no stale success. |
| Workload on person-only endpoint, person on workload-only endpoint | Refused in either direction. |
| Unknown `contractVersion="2"` | Refused. |
| PAT label claiming authenticated agent identity | Declared client only; no Actor authority. |
| Verified token `act=agent-B`; grant actor is `agent-A` | Refused; no caller constructed. |
| Grant reference supplied in request input, not the verified credential/mapping | Refused; no caller constructed. |
| `cnf.jkt`-bound token without a DPoP header | 401; no caller constructed, no plain-bearer downgrade. |
| `cnf.jkt`-bound token with wrong proof key thumbprint | 401; no caller constructed. |
| `cnf.jkt`-bound token with replayed proof `jti` | 401; no caller constructed. |
| More than one credential, or a credential in the URI query | Refused before caller construction. |

Machine-readable group: [principals and workload cache](../../Source/Identity.Specs/Vectors/arc-verified-caller-v1/principals-and-workload.json), repository path `Source/Identity.Specs/Vectors/arc-verified-caller-v1/principals-and-workload.json`. These are data vectors, not executed runtime conformance tests. `when.action` names a logical operation; outcomes and reasons are vector vocabulary, not public error messages.

## Open questions

- **M10 / W0:** lock workload allowlist mapping and measured deletion latency. Thirty seconds is a placeholder; ten-minute tokens are proposed.
- **W0 / D20:** lock policy freshness and epoch re-evaluation mechanics, plus non-secret workload/runner credential identifiers.
- **(align with #24):** #24 defines a string Contract version, account key separate from product Person, Credential kind categories, structured Client, Sender binding, assurance fields, and conditional integer Policy epoch. This draft has adopted those constraints. Exact camel-case serialization, `principalKind`, `servicePrincipal`, and `permissions` are additional Arc mechanics; machine-principal and runner credentials and enum spellings still need review. Actor remains person-representing, not the name for a bare service principal. Environment wire syntax and Actor namespaces remain open in #24.
- **(align with #24):** `grantId` is a record reference, not an approved extra access-token claim. #24's claim allowlist requires explicit review before such a claim is added; `jti` is not a grant id.
- **Runner contract:** registration, session expiry, rotation, and per-work revocation are outside this record. No new default is invented here.

## Versioning

A Locked spec MUST change only through a new version file. Consumers MUST reject unknown record profiles, never infer compatibility from field names, and retain the superseded document and vectors. New versions MUST identify their predecessor and migration requirements. A new verifier MUST preserve authenticated actors, represented persons, and declared clients as distinct concepts.

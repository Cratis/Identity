# Scope catalog

```text
Spec id: arc-scope-catalog
Version: 1
Status: Signed off
Signed off: 2026-11-05 by woksin, https://github.com/Cratis/Identity/issues/14; owner confirmation pending
Issue: https://github.com/Cratis/Arc/issues/3700
Sign-off issue: https://github.com/Cratis/Identity/issues/14
Supersedes: None
```

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Purpose

The catalog connects a resource, its token audience, and its scope vocabulary. It prevents a scope name from becoming authority outside the resource that owns it. This specification is not a statement of deployed registrations.

## Terms

Person, Account, Realm, Issuer, Subject, Linked key, Resource, Resource indicator, Audience, Tenant, Organisation, Environment, Actor, Credential kind, Assurance, Policy epoch, Access token, ID token, Refresh token, and PAT have exactly the meanings in the Terms table of [identity-contract-v1.md](identity-contract-v1.md), Cratis/Identity#24. Shared meanings are not redefined here.

A **catalog entry** binds one resource indicator to its audience and permitted scope names. A **bundle** is a reviewed, versioned selection of scopes for consent. An **exchange mapping** explicitly relates scopes at two resources without granting additional authority.

## Normative contract

MUST, MUST NOT, and SHOULD use [RFC 2119](https://www.rfc-editor.org/rfc/rfc2119) meanings. Requirements apply after sign-off.

### N1 catalog

All URLs in this table are test fixtures, not production endpoints. Names are decided under q9 at the Identity #14 sign-off. Resource indicators and audience strings are distinct, as defined in identity-contract-v1 §4. Audience strings MUST follow that contract's 1–64-character catalog syntax, not use resource URLs.

| Resource | Resource indicator | Audience | Scope prefix | Stable scope set |
| --- | --- | --- | --- | --- |
| Direct MCP | `https://direct.example.test/mcp` | `direct-mcp` | `direct:` | `direct:read`, `direct:write` |
| Direct REST API | `https://direct.example.test/v1` | `direct-api` | `direct:` | `direct:read`, `direct:write` |
| Studio | `https://studio.example.test/mcp` | `studio-api` | `studio:` | `studio:read`, `studio:write` |
| Generated Atlas app | `https://atlas.example.test/mcp` | `stage-atlas-api` | `stage-atlas:` | `stage-atlas:read`, `stage-atlas:write` |

The scope catalog MUST map each registered RFC 8707 resource indicator URL to exactly one audience slug, and each audience slug to exactly one resource indicator URL. This one-to-one mapping MUST determine the token's `aud`: for example, `https://direct.example.test/mcp` maps to `direct-mcp`, and the Direct REST API resource `https://direct.example.test/v1` maps to `direct-api`. `direct-api` is the REST-only audience and `direct-mcp` is the MCP-only audience; a token for one MUST NOT be accepted by the other's endpoint. `aud` MUST NOT be a URL. Each deployment MUST register its actual absolute resource indicator and audience slug. Each generated Stage app MUST have an application-specific registration and scope prefix; it MUST NOT reuse another app's authority by using a shared `stage:` scope. A token MUST target one resource. Verifiers MUST reject audiences not registered for the receiving resource and MUST NOT use URL similarity, hostname suffixes, or scope prefixes as audience verification.

N1 does not prescribe a `/v1` resource. The reference host additionally registers `https://cratis.direct/v1` with the distinct audience `direct-v1` and its existing `direct:read`, `direct:content.write` and `direct:work` scopes. Its `direct-mcp` entry binds `https://cratis.direct/mcp` to `direct:mcp:read`, `direct:mcp:content.write` and `direct:mcp:work`, preserving N1's MCP audience. These runtime scope sets differ from N1's stable scope set above. API and MCP have distinct scope sets; clients requesting MCP must select its indicator and scopes. `GrantPolicy` resolves explicit indicators before filtering scopes. Changing configuration does not exchange existing grants or imply authority across resources. The local Compose tenant fixture is configured separately on both entries.

The catalog MUST map every exposed operation to its required scopes. `read` MUST cover only operations reviewed as non-mutating; `write` MUST cover only explicitly exposed mutations. New operations MUST remain unexposed until explicitly allowlisted. A scope name MUST NOT expose a new command automatically. `write` MUST NOT imply `read` unless an explicit reviewed catalog mapping says so; this specification instead uses bundles containing both.

Unknown scopes and wildcard scopes MUST be refused. Scope prefix and operation syntax MUST match identity-contract-v1 §4. There MUST NOT be an `approve` or `admin` scope. A requested bundle MUST expand to its exact versioned scope list at consent time. A later bundle expansion MUST NOT widen an existing grant. Catalog changes affecting operation coverage MUST receive security review and MUST NOT silently widen existing permissions.

### Reviewed bundles and runtime checks

Bundle `reader-v1` contains the resource's `read` scope. `editor-v1` contains its `read` and `write` scopes. Bundle publication MUST include owner review, the exact scope list, version, and operation coverage. These bundles were approved at the Identity #14 sign-off.

An operation MUST satisfy all applicable checks: current product membership and roles, required token scopes, tenant and environment binding, applicable grant bounds, assurance, and current policy fences. Scopes MUST act only as a ceiling. Neither a scope nor consent MUST create membership or override product authorization.

Cross-resource [delegation](arc-delegation-v1.md) MUST use explicit catalog mappings reviewed by both resource owners. A string subset, equal suffix, or broad intermediate scope MUST NOT establish a mapping. The target MUST recheck its own product policy. A token for Direct MUST NOT be forwarded as a Studio token.

## Test vectors

Synthetic person `person-fixture` is a member of tenant `tenant-fixture` with a read-only product role unless stated otherwise.

| Input | Expected result |
| --- | --- |
| Direct audience, `direct:read`, permitted Direct query | Permitted if remaining policy checks pass. |
| Direct audience, `direct:write`, read-only role, Direct mutation | Refused: token scope does not grant a missing role permission. |
| Studio audience and `studio:read` presented to Direct | Refused for audience mismatch. |
| `direct-mcp` token presented to the Direct REST API, or `direct-api` token presented to the Direct MCP endpoint (`/mcp`) | Refused for audience mismatch in both directions. |
| Direct audience and `studio:read` | Refused for scope/catalog mismatch. |
| Atlas audience and `stage-atlas:read` at a different app | Refused for audience mismatch. |
| `direct:*`, or an unregistered `direct:admin` | Refused as an unknown scope. |
| Consent to `reader-v1`, then publication of `editor-v1` | Existing grant still contains only `direct:read`. |
| Direct read scope exchanged for Studio write with no explicit mapping | Refused; names do not establish attenuation. |

Machine-readable group: [catalog and policy](../../Source/Identity.Specs/Vectors/arc-scope-catalog-v1/catalog-and-policy.json), repository path `Source/Identity.Specs/Vectors/arc-scope-catalog-v1/catalog-and-policy.json`. The group has `name`, `description`, `given`, `when.action`, and `then`; `given.vectors` contains individual cases. These are data vectors, not executed runtime conformance tests. Outcomes and reasons are vector vocabulary, not public error messages.

## Decisions recorded at sign-off

Decided in Identity #14 (2026-11-05) with the recommended answers from the sign-off package:

- **q9 / N1:** production resource indicators, audiences, scope names, operation coverage, and bundle ownership are decided as the sets above; test URLs remain fixtures.
- **D7:** generated-app exposure remains an explicit allowlist; approval-related syntax depends on the approval contract and is not supplied by a write scope.
- **(align with #24):** audience strings, scope syntax, immutable bindings, and one audience per access token follow #24. This specification corrected URL-shaped audiences to catalog strings. #24 delegates exact catalog registrations, operation coverage, bundle versioning, and exchange mappings to Arc; consumer review of these values was covered by the Identity #14 sign-off.

## Versioning

This spec MUST change only through a new version file. Catalog and bundle revisions MUST preserve previously consented scope ceilings; any widening MUST require explicit renewed consent and product authorization. Unknown catalog versions MUST NOT result in permissive authorization.

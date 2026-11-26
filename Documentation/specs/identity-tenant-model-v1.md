---
description: "Identity tenant model specification, version 1: the names it adds for organisations, workspaces and namespaces on top of identity-contract-v1."
---

# Tenant model: organisation, workspace and namespace

```text
Spec id: identity-tenant-model
Version: 1
Status: Signed off
Signed off: 2026-11-05 by woksin, https://github.com/Cratis/Identity/issues/14; owner confirmation pending
Issue: https://github.com/Cratis/Identity/issues/25
Sign-off issue: https://github.com/Cratis/Identity/issues/14
Supersedes: None
```

## Terms

The [Terms of identity-contract-v1](identity-contract-v1.md#terms) apply **verbatim**; this document does not redefine them. It adds only the names below.

| Term | Meaning |
|---|---|
| Workspace | The product-facing name of a Tenant. It is not a separate identifier ([contract §4](identity-contract-v1.md#4-resource-tenant-and-organisation)). |
| Namespace | The Chronicle storage partition of one product installation. |
| Namespace mapping | The installation's registered tenant-to-namespace resolution. The product owns it. |
| Tenant binding | The events-catalogue Binding `(resource, installation, tenant, environment, organisation)` ([events §2.2](identity-events-v1.md#22-stream-boundaries)). |
| Credential constraints | The tenant, resource and environment fixed in a credential. |
| Requested target | The resource, installation, environment and tenant that AuthProxy or the application resolves from trusted routing. |
| Organisation administrator, tenant administrator | A person who administers the organisation in Access, or the tenant in the product. Neither role is membership of the other. |

## 1. Scope and status

Signed off in [Identity #14](https://github.com/Cratis/Identity/issues/14) on 2026-11-05 under D5 = option A (Identity #23) and D2 = product callback. This document consolidates [contract §4](identity-contract-v1.md#4-resource-tenant-and-organisation) and does not change it. It defines no wire syntax and claims no deployed behaviour. Enforcement work is Cratis/AuthProxy#253 (target check) and the product and Arc verifiers.

## 2. Three concepts

| Concept | Owner | Identifier | Holds | Never |
|---|---|---|---|---|
| **Organisation** | Access | Opaque reference, carried as `org` | Verified domains, connections, policies, confirmed tenant bindings | Membership or roles |
| **Workspace** (tenant) | The product | Tenant id ([contract §4](identity-contract-v1.md#4-resource-tenant-and-organisation)), carried as `tid` | Membership, roles, business data | A global identifier |
| **Namespace** | The product installation | Storage name | Chronicle data of one tenant | A caller input or a user-visible name |

- An organisation MUST NOT hold membership or roles.
- `org` MUST NOT be inferred from `tid`, and `tid` MUST NOT be inferred from `org` ([contract §5](identity-contract-v1.md#5-token-claims)). Nothing in this document uses `org` to authorize.
- A tenant id is scoped by resource and installation, not globally.
- A namespace MUST NOT be accepted from caller input, shown to users, or used to infer a tenant id.
- UI labels MUST NOT conflate the three identifiers.
- The subject MUST NOT encode the tenant ([contract §2.1](identity-contract-v1.md#21-subject)). The tenant travels only as `tid` or as a credential constraint.

## 3. Ownership (D5 = A)

| Fact | Sole writer | Readers |
|---|---|---|
| Organisation identity, verified domains, connections, organisation policies, policy epoch, tenant bindings | Access ([events §8](identity-events-v1.md#8-event-catalogue-v1)) | Identity, AuthProxy, products through public events |
| Tenant existence, membership, roles, business authorization | The product | The product; Identity and AuthProxy through the callback in §5 |
| Account and person key | Identity ([contract §2](identity-contract-v1.md#2-person-account-and-identifiers)) | All |
| Namespace mapping | The product installation | The product |

- Access MUST NOT write membership, roles, invitations or business authorization. Products MUST NOT write Access facts.
- There is no universal membership directory. Option (c) of D5 was rejected because it would create a second membership writer.
- Organisation administration MUST NOT be treated as product membership or a product role. Products MUST keep explicit authorization checks.

## 4. Tenant bindings

- A binding is confirmed only by authenticated confirmations from both an organisation administrator and a tenant administrator, never by bearer confirmation links ([events §8](identity-events-v1.md#8-event-catalogue-v1)).
- An unconfirmed binding confers nothing.
- Without a confirmed binding, no organisation policy applies and the caller `organisation` is absent ([verified-caller](arc-verified-caller-v1.md)).
- A binding MUST NOT create membership, selectability or namespace selection.
- Removal is the Access fact `TenantBindingRemoved`, handled as a restricting change per [identity-revocation-v1 §3.1](identity-revocation-v1.md#31-domain-facts). This document defines no further removal semantics.

## 5. Membership and tenant lists (D2)

- Selectable tenants MUST come from the product's authenticated account-key callback registered per resource.
- A list, selection or cached answer MUST NOT authorize.
- The product MUST check live membership and roles on every request.
- Configuration-only directories are development fixtures.
- Scopes are ceilings and never create membership ([scope catalog](arc-scope-catalog-v1.md)).

## 6. Credential constraints

- Tenant, resource and environment are fixed when the credential is created: the PAT record ([arc-pat-format](arc-pat-format-v1.md)), the access token `aud` and `tid`, the grant (events `AuthorizationGranted`), and a session once a tenant is selected.
- They MUST NOT change for an existing credential. Refresh and delegation MUST NOT widen them.
- Environment comes from the trusted catalog or installation and the credential record, never from a caller label. Its wire syntax is not defined here.
- A session without a selected tenant reaches only login, tenant listing and selection.
- Switching tenant MUST rerun selection, live membership and policy checks, and MUST yield a credential for the new tenant. It MUST NOT rewrite `tid`.
- Membership in another tenant MUST NOT widen a credential.

## 7. Request resolution and where checks happen

A request MUST be resolved in this order:

1. Verify the credential with its verifier ([verified-caller](arc-verified-caller-v1.md), [forwarded-identity v2](arc-forwarded-identity-v2.md), [PAT format](arc-pat-format-v1.md)) and read its constraints.
2. Resolve the requested target independently from trusted routing: resource and installation from the catalog or host, environment from the installation registration, tenant from the registered route, host, tenancy header or tool argument. Caller input identifies the target only; it is never a constraint.
3. Compare resource, environment and tenant exactly. Any mismatch MUST be refused before any downstream call, namespace selection or membership lookup. Neither value overwrites the other, and there is no default-tenant fallback. AuthProxy checks the credentials it verifies (OAuth bearer, browser and on-behalf sessions). The application checks PATs it verifies after dispatch, and every credential again at caller construction.
4. Construct the caller with tenant = credential tenant = target tenant.
5. Select the namespace only from the verified tenant through the namespace mapping. Never use caller strings, the organisation or `org`.
6. Check live membership, roles, scopes and policy before the operation executes. A refusal MUST NOT execute the operation. Whether step 6 reads before or after step 5 is the product's choice.

Machine and workload callers take the tenant from a checked per-work binding ([verified-caller](arc-verified-caller-v1.md)). Response codes are defined by each verifier's spec, not here.

## 8. Out of scope

| Item | Owner |
|---|---|
| Organisation id syntax and any authorization use of `org` | [Contract §5](identity-contract-v1.md#5-token-claims); Access module, Identity #43 |
| Binding workflow, confirmation UX and cardinality | Identity #44, Identity #63 |
| Policy enforcement points | Identity #64 |
| Policy freshness and epochs | Identity #33; D20 |
| Environment wire syntax | A separate versioned contract per D20 ([contract §10](identity-contract-v1.md#10-decisions-recorded-at-sign-off)) |
| SCIM provisioning | Identity #31 |
| Refusing a credential whose tenant, resource or environment differs | Cratis/AuthProxy#253 |
| Per-product namespace naming | The product |
| Public vocabulary | Cratis/Documentation#265 |

## 9. Machine-readable vectors

[Data vectors](../../Source/Identity.Specs/Vectors/identity-tenant-model-v1/) contain one JSON object per file with `name`, `description`, `given`, `when` and `then`. Inputs describe trusted stored state and verified authentication evidence unless explicitly stated otherwise. `when.action` names a logical operation, not an API endpoint. `then` gives required assertions; outcomes and reason strings are vector vocabulary, not public error messages. Example hosts and ids are synthetic, including `org-ref-1`, which is an opaque reference and not a syntax.

These are **data only**. They add no C# tests or project configuration and do not demonstrate runtime conformance.

- `cross-tenant-credential-refused-before-namespace.json`: a PAT for t-alpha is refused by the application for a t-beta target.
- `member-of-both-tenants-no-cross-tenant-access.json`: membership in both tenants does not widen a token; AuthProxy refuses.
- `tenant-switch-reruns-selection.json`: switching reruns selection and yields a new credential.
- `resource-or-environment-mismatch-refused.json`: a different resource or environment is refused.
- `organisation-admin-is-not-membership.json`: organisation administration does not make a tenant selectable.
- `membership-removed-after-selection-refused.json`: live membership is checked; a cached list does not authorize.

## 10. Decisions recorded at sign-off

- **D5 = A** (Identity #23): Access owns organisation identity, verified domains, connections, policies and confirmed tenant bindings; products are the only authority for membership, roles and business authorization. Option (c) is rejected because it would create a second membership writer.
- **D2 = product callback.**
- **Lock:** Identity #14, 2026-11-05.

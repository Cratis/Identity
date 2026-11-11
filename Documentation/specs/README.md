# Specifications

Specifications define contracts shared by Identity and its consumers. RFC 2119 keywords **MUST**, **MUST NOT**, **REQUIRED**, **SHOULD**, **SHOULD NOT** and **MAY** express normative requirements. In a Draft, they describe the proposed contract, not an already deployed or approved implementation. Locked specifications record the signed-off contract and change only through a new version file, as the conventions below require.

## Index

| Spec id | Version | Status | Document |
|---|---|---|---|
| identity-contract | 1 | Signed off | [Person, account, resource, tenant and token contract](identity-contract-v1.md) |
| identity-events | 1 | Signed off | [Identity event rules and catalogue](identity-events-v1.md) |
| identity-events | 1.2 | Draft | [Default event type ids amendment](identity-events-v1.2-amendment.md) |
| identity-tenant-model | 1 | Signed off | [Tenant model: organisation, workspace and namespace](identity-tenant-model-v1.md) |
| identity-access-token | 1 | Signed off | [JWT access-token profile](identity-access-token-v1.md) |
| identity-credential-record | 1 | Draft | [Credential record and adapter boundary](identity-credential-record-v1.md) |
| identity-dpop | 1 | Signed off | [DPoP edge profile](identity-dpop-v1.md) |
| identity-revocation | 1 | Signed off | [Revocation protocol and fence durability](identity-revocation-v1.md) |
| identity-standards-references | 1 | Signed off | [Verified standards references](identity-standards-references-v1.md) |
| arc-pat-format | 1 | Signed off | [PAT format](arc-pat-format-v1.md) |
| arc-scope-catalog | 1 | Signed off | [Scope catalog](arc-scope-catalog-v1.md) |
| arc-verified-caller | 1 | Signed off | [Verified caller and credential matrix](arc-verified-caller-v1.md) |
| arc-delegation | 1 | Signed off | [Delegation](arc-delegation-v1.md) |
| arc-mcp-authorization | 1 | Signed off | [MCP authorization](arc-mcp-authorization-v1.md) |
| arc-forwarded-identity | 2 | Signed off | [Forwarded identity v2](arc-forwarded-identity-v2.md) |

## Conventions

- Specification files MUST be named `<area>-<name>-v<major>.md`. Area prefixes are `identity-` for this repository's Identity specs and `arc-` for Arc specs hosted here. For example, `identity-contract-v1.md` has the vector directory `identity-contract-v1`; the directory name MUST remain aligned with the specification stem.
- Each specification MUST begin with a title and this header block (with its own values):

  ```text
  Spec id: identity-contract
  Version: 1
  Status: Draft
  Issue: https://github.com/Cratis/Identity/issues/24
  Sign-off issue: https://github.com/Cratis/Identity/issues/14
  Supersedes: None
  ```

- Status MUST be `Draft`, `Signed off` or `Locked`. A Draft MUST identify outstanding decisions and reviews; it MUST NOT imply sign-off.
- A specification is Signed off when it has passed the reviews it requires and a maintainer has signed it off in [Identity #14](https://github.com/Cratis/Identity/issues/14), but the owner has not yet confirmed that sign-off there. It MUST add a `Signed off: <date> by <maintainer>, <sign-off link>; owner confirmation pending` line directly after `Status:`.
- A specification becomes Locked only after the reviews it requires, maintainer sign-off and the owner's confirmation, all recorded in [Identity #14](https://github.com/Cratis/Identity/issues/14). A Signed-off specification becomes Locked without any content change. A Locked specification MAY add a `Locked: <date>, <sign-off link>` line directly after `Status:` recording when it was locked.
- A Signed-off or Locked specification MUST change only through a new version in a new file. That version MUST identify what it supersedes and any migration requirements; the old document and its vectors MUST remain available.
- Machine-readable examples MUST identify their specification version through their directory and describe inputs, action and expected outcome. Data vectors do not imply an implemented or passing conformance suite.

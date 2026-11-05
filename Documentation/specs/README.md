# Specifications

Specifications define contracts shared by Identity and its consumers. RFC 2119 keywords **MUST**, **MUST NOT**, **REQUIRED**, **SHOULD**, **SHOULD NOT** and **MAY** express normative requirements. In a Draft, they describe the proposed contract, not an already deployed or approved implementation.

## Index

| Spec id | Version | Status | Document |
|---|---|---|---|
| identity-contract | 1 | Draft | [Person, account, resource, tenant and token contract](identity-contract-v1.md) |
| identity-events | 1 | Draft | [Identity event rules and catalogue](identity-events-v1.md) |
| identity-access-token | 1 | Draft | [JWT access-token profile](identity-access-token-v1.md) |

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

- Status MUST be `Draft` or `Locked`. A Draft MUST identify outstanding decisions and reviews; it MUST NOT imply sign-off.
- A specification becomes Locked only after the reviews it requires and sign-off recorded in [Identity #14](https://github.com/Cratis/Identity/issues/14).
- A Locked specification MUST change only through a new version in a new file. That version MUST identify what it supersedes and any migration requirements; the old document and its vectors MUST remain available.
- Machine-readable examples MUST identify their specification version through their directory and describe inputs, action and expected outcome. Data vectors do not imply an implemented or passing conformance suite.

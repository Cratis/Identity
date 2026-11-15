# Specifications

Specifications define contracts shared by Identity and its consumers. RFC 2119 keywords **MUST**, **MUST NOT**, **REQUIRED**, **SHOULD**, **SHOULD NOT** and **MAY** express normative requirements. In a Draft, they describe the proposed contract, not an already deployed or approved implementation. Locked specifications record the signed-off contract and change only through a new version file, as the conventions below require.

## Coverage maps

- [Container and kernel contract map](container-kernel-contract-map.md): assertion coverage, retained platform candidates and automatic CI selection. This inventory is not a versioned protocol specification.

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
| identity-access-freshness | 1 | Draft | [Access freshness and policy epochs](identity-access-freshness-v1.md) |
| identity-federation | 1 | Draft | [Federation contract and issuer-scoped relink](identity-federation-v1.md) |
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

## Vector artifact

Every vector folder under `Source/Identity.Specs/Vectors/` ships in one versioned bundle, `identity-contract-vectors`, released from a `vectors/identity-contract-vectors/v<semver>` tag as `identity-contract-vectors-<semver>.tar.gz` with a `.sha256` file. Its `manifest.json` lists every file with its SHA-256, the per-folder file counts and a `coverage` object saying which folders Identity executes and which are not applicable to Identity. The bundle carries the unreleased `arc-mcp-authorization` 2.1.0 overlay under `arc-mcp-authorization-v1-overlay-2.1.0/`. Folders are discovered by name, so a new `<area>-<name>-v<major>` folder is bundled automatically.

- Run `node scripts/package-contract-vectors.mjs check` to verify the tree, manifest, coverage, changelog and baselines against released tags, and `write` to regenerate the manifest after a vector change. Add a changelog entry and bump the version for any change; a released version and its baseline never change.
- The tables below are informational and not part of any normative contract. They name, per Signed-off contract, what each implementer runs and the artifact version it pins; a row says what an implementer runs, not that it passes. "To be confirmed" means the implementer repository has not confirmed the fact here. Draft specifications carry their own "Implementers and pinned vector version" section instead.

  | Contract | Implementer | Runs | Pinned vector version | Not applicable (reason) |
  |---|---|---|---|---|
  | arc-delegation-v1 | Identity | Data only; no executable reader | Tree; bundle 1.0.0 once tagged | Data vectors only, no reader |
  | arc-delegation-v1 | Arc, AuthProxy | To be confirmed | To be confirmed | To be confirmed |
  | arc-forwarded-identity-v2 | Identity | Data only; no verifier | Tree; bundle 1.0.0 once tagged | Arc's generator exports these cases; Identity does not verify envelopes |
  | arc-forwarded-identity-v2 | Arc | `Arc.Core.Specs/Identity/ForwardedIdentityV2/when_verifying_shared_vectors.cs` (reported from Arc code search; to be confirmed) | Vendored copy, no version tag | To be confirmed |
  | arc-forwarded-identity-v2 | AuthProxy | `for_V2/when_exercising_the_published_vectors.cs` (reported from AuthProxy code search; to be confirmed) | Vendored copy, no version tag | To be confirmed |
  | arc-mcp-authorization-v1 | Identity | Executable: `Source/Identity.Specs/for_ArcMcpVectors/` and `for_VectorArtifacts/` (2.0.0 and the 2.1.0 overlay) | Per-spec artifacts 2.0.0 and 2.1.0; also in bundle 1.0.0 once tagged (2.1.0 under `arc-mcp-authorization-v1-overlay-2.1.0/`) | None |
  | arc-mcp-authorization-v1 | Arc | `Arc.Mcp.Specs/for_McpVectors` (reported from Arc code search; to be confirmed) | 2.0.0 at tag `vectors/arc-mcp-authorization/v2.0.0` | To be confirmed |
  | arc-mcp-authorization-v1 | AuthProxy | `PublishedAuthorizationVectors.cs` and an ownership table (reported from AuthProxy code search; to be confirmed) | 1.0.0 at tag `vectors/arc-mcp-authorization/v1.0.0`; behind current, pin bump pending | Kept in AuthProxy's own ownership table; to be confirmed |
  | arc-pat-format-v1 | Identity | Data only; no executable reader | Tree; bundle 1.0.0 once tagged | Data vectors only, no reader |
  | arc-pat-format-v1 | Arc | `PersonalAccessTokens.Specs/Vectors/format-v1.json` (reported from Arc code search; to be confirmed) | Copy linking Identity commit `bc7402b9`; no version tag | To be confirmed |
  | arc-pat-format-v1 | AuthProxy | To be confirmed | To be confirmed | To be confirmed |
  | arc-scope-catalog-v1 | Identity | Executable: `Source/Identity.Specs/for_VectorArtifacts/when_applying_the_scope_catalog_versions.cs` | Tree; bundle 1.0.0 once tagged | None |
  | arc-scope-catalog-v1 | Arc, AuthProxy | To be confirmed | To be confirmed | To be confirmed |
  | arc-verified-caller-v1 | Identity | Data only; no executable reader | Tree; bundle 1.0.0 once tagged | Data vectors only, no reader |
  | arc-verified-caller-v1 | Arc, AuthProxy | To be confirmed | To be confirmed | To be confirmed |
  | identity-access-token-v1 | Identity | Data only; no executable reader yet | Tree; bundle 1.0.0 once tagged | Data only, not executable specs |
  | identity-access-token-v1 | Arc, AuthProxy | To be confirmed | To be confirmed | To be confirmed |
  | identity-contract-v1 | Identity | Executable: `Source/Identity.Specs/Accounts/given/a_contract_vector.cs` | Tree; bundle 1.0.0 once tagged | None |
  | identity-contract-v1 | Arc, AuthProxy | To be confirmed | To be confirmed | To be confirmed |
  | identity-dpop-v1 | Identity | Executable: `Source/Identity.Specs/for_DPoPProofValidator/when_replaying_the_as_header_vectors.cs` | Tree; bundle 1.0.0 once tagged | None |
  | identity-dpop-v1 | Arc, AuthProxy | To be confirmed | To be confirmed | To be confirmed |
  | identity-events-v1 | Identity | Executable: `Source/Identity.Specs/for_EventContentRules/when_applying_locked_vectors.cs` | Tree; bundle 1.0.0 once tagged | None |
  | identity-revocation-v1 | Identity | Data only; no executable reader | Tree; bundle 1.0.0 once tagged | Data only, not implemented or passing executable specs |
  | identity-revocation-v1 | Arc, AuthProxy | To be confirmed | To be confirmed | To be confirmed |
  | identity-tenant-model-v1 | Identity | Data only; no executable reader | Tree; bundle 1.0.0 once tagged | No executable reader |
  | identity-tenant-model-v1 | Arc, AuthProxy | To be confirmed | To be confirmed | To be confirmed |
- The per-spec `arc-mcp-authorization` artifacts and their tags are unchanged.

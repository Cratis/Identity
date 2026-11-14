# Identity contract vectors bundle changelog

Versions follow SemVer. A change to any bundled vector file, or adding or removing a folder, requires a new version, a new entry here and a new baseline. Released versions and their baselines never change.

## 1.0.0

First release of the bundle: every vector folder under `Source/Identity.Specs/Vectors/` in one checksummed archive, `identity-contract-vectors-1.0.0.tar.gz`.

- Includes all 15 vector folders: `arc-delegation-v1`, `arc-forwarded-identity-v2`, `arc-mcp-authorization-v1` (with its own manifest, changelog and baselines, unchanged), `arc-pat-format-v1`, `arc-scope-catalog-v1`, `arc-verified-caller-v1`, `identity-access-freshness-v1`, `identity-access-token-v1`, `identity-contract-v1`, `identity-credential-record-v1`, `identity-dpop-v1`, `identity-events-v1`, `identity-federation-v1`, `identity-revocation-v1` and `identity-tenant-model-v1`.
- Carries the unreleased `arc-mcp-authorization` 2.1.0 overlay under `arc-mcp-authorization-v1-overlay-2.1.0/`, next to the 2.0.0 base in `arc-mcp-authorization-v1/`.
- `manifest.json` lists every file with its SHA-256, the per-folder file counts, and a `coverage` object stating which folders Identity executes and which cases are not applicable to Identity.
- The per-spec `arc-mcp-authorization` 2.0.0 and 2.1.0 artifacts and their tags are unchanged.

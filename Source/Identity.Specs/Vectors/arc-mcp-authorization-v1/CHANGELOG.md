<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Arc MCP authorization vector artifact changelog

## 1.0.0

- Package the 48 signed-off MCP authorization cases without changing vector bytes.
- Assign stable case IDs in the manifest, with zero-based indexes and SHA-256 bindings.
- Record the MCP authorization revision, contract version, and vector file hash.

Artifact versions use SemVer independently of the contract version. Additive cases
increment the minor version; meaning changes, removals, or incompatible schema changes
increment the major version; documentation and packaging corrections increment the patch
version. Never reassign an existing case ID to different behavior. Every artifact bump
updates this changelog, the manifest, and the contract document together.

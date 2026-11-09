<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Event content and schema compatibility check

The ordinary solution build runs this non-packable executable after compiling
all production projects (Contracts, Identity, and Host). A content violation returns exit code 1 and fails MSBuild. No service,
database, Docker container, or secret-store access is needed.

```sh
dotnet build Identity.slnx -c Release -warnaserror
```

For previously built Release assemblies, run the same checker directly from the
repository root. Supply every production output, followed by the solution path:

```sh
dotnet Source/Identity.Contracts.Check/bin/Release/net10.0/Cratis.Identity.Contracts.Check.dll \
  Source/Identity.Contracts/bin/Release/net10.0/Cratis.Identity.Contracts.dll \
  Source/Identity/bin/Release/net10.0/Cratis.Identity.dll \
  Source/Identity.Host/bin/Release/net10.0/Cratis.Identity.Host.dll \
  Identity.slnx
```

The checker runs on every build, including incremental builds, so solution or
project changes cannot reuse a stale coverage result. Its project references
supply the assembly paths to MSBuild. A solution-level guard requires an output
for every project except the checker itself and `*.Specs.csproj` projects.
When adding a production project to `Identity.slnx`, also add a project reference
in `Identity.Contracts.Check.csproj`; otherwise the build fails with
`production_assembly_not_scanned`, even if the new project has no events yet.
An event registered outside Contracts fails catalogue validation. The Contracts
package does not depend on the executable.

## Adding an event

Allocate the explicit event type id in
[identity-events-v1](../../Documentation/specs/identity-events-v1.md) before the
first append. Keep the area, visibility, required fields, and PII marks consistent
with the catalogue. Transcribe the row into
[catalogue/identity-events-v1.json](catalogue/identity-events-v1.json), including
its section 4.1 source-fact mapping when applicable. Null ids represent unimplemented
catalogue rows, not permission to use an implicit id. Specs check both tables for
drift, and the executable requires every allocated row to be present.

The reflection adapter examines public instance properties and fields recursively,
including aliases, inherited members, typed collections, concepts, current and
prior generations, and non-public event artifacts. It rejects open payload slots.
PII metadata is resolved using Chronicle's provider. Account-stream subject
resolution comes from the catalogue's exact CLR identity, not a namespace-wide
assumption. New subject-resolution mechanisms require review before use. There
are currently no explicitly registered event artifacts outside attribute discovery;
any such registration must also be supplied to discovery.

## Persisted schema compatibility

The same executable, discovery, and `ReflectionShapeBuilder` metadata enforce
[catalogue/event-schemas.json](catalogue/event-schemas.json). This is an append-only
history keyed by event type id and generation, not a second event catalogue.
Each row records the allocation's assembly, namespace, CLR name, visibility,
recursive fields (CLR/wire names, exact member types, concept primitive, PII and
nullability), and an embedded JSON fixture. The initial history covers all 31
currently compiled events, including source facts and revocation events.

Do not edit or remove an existing schema row or fixture to accommodate a code
change. Updating the current field list in the event catalogue alone does not
make an incompatible persisted generation valid. Same-generation changes,
removed events or generations, changed ids or visibility, unrecorded generations,
gaps, and missing fixtures fail the ordinary build with `event-schema` diagnostics.
CLR names and namespaces are allocation metadata: previous-generation records
may be renamed or relocated while retaining their assembly and persisted shape.

For a new event, append its generation-one schema and add synthetic JSON at
`fixtures/<type-id>/1.json`. Use primitive concept values and Chronicle's wire
naming; fixtures must never contain real personal data, tokens, or credentials.
For a persisted schema change, keep the explicit id, advance the current
`[EventType]` generation by one, and retain every previous record with
`[EventTypeGenerationFor]`. Append (never replace) the new schema and fixture.
Update the current catalogue and implement Chronicle's migration contract with
fixture-based replay specs for each adjacent transition. Prior records are checked
against their retained schemas rather than the current catalogue's field list;
they still run through the shared content and PII checks.

The fixture specs deserialize and round-trip every discovered generation using
Chronicle's `EventSerializer` and concept converter factories. No production event
currently has generation greater than one, so there is no real migration chain to
exercise yet; the first bump must supply its actual adjacent migration and replay
specs, not placeholder migration infrastructure.

```sh
dotnet test Source/Identity.Specs/Identity.Specs.csproj -c Release --filter FullyQualifiedName~for_EventSchemaCompatibility
```

## Released-ref immutability gate

The ordinary build compares declarations with the embedded manifest. To prevent a
change from rewriting code, manifest rows, and fixtures together,
`.github/workflows/build.yml` runs the standalone released-ref mode **after** the
Release warnings-as-errors build and format verification. For example, locally:

```sh
dotnet Source/Identity.Contracts.Check/bin/Release/net10.0/Cratis.Identity.Contracts.Check.dll --released-ref origin/main Identity.slnx
```

This mode resolves the requested ref to a commit with `git rev-parse`, reads its
catalogue with `git show`, and enumerates its entire fixture tree with
`git ls-tree` before reading each blob. It compares that trusted state with the
current on-disk catalogue and fixture tree, not potentially stale embedded files.
Released rows cannot change or disappear; recursive field order is insignificant.
Every released fixture, including unreferenced fixtures, must retain exactly the
same bytes and path. New rows and fixtures are allowed. Diagnostics never print
fixture contents.

A missing/unresolvable ref, unavailable Git, unreadable or malformed catalogue,
or empty released catalogue or fixture set fails closed in the checker. There is
no environment variable skip.

The workflow checks out full history (`fetch-depth: 0`). For a pull request, it
validates and fetches the base branch into `origin/<base>`; for a push to main, it
uses `github.event.before`, fetching that commit if necessary. A first push with
no previous commit emits a notice and skips this comparison. Event name, PR base,
and previous commit are passed through step environment variables, never
interpolated into shell source. The selected base is resolved to a commit before
checking it, so subsequent ref movement cannot change the comparison.

The workflow has one narrow bootstrap exemption for landing this initial schema
baseline. It tests the resolved base with:

```sh
baseline="Source/Identity.Contracts.Check/catalogue/event-schemas.json"
git cat-file -e "$base_commit:$baseline"
```

Only if that fails **and** a successful `git ls-tree --full-tree` lookup confirms
that the baseline path has no tree entry does the step emit a clear bootstrap
notice and skip. An existing but unreadable blob, unresolved ref, failed fetch or
tree lookup, missing Git, and every checker error fail the job. An empty or invalid
baseline is not an exemption. As soon as the baseline exists in the trusted base,
this bootstrap exemption no longer applies. Otherwise the workflow runs:

```sh
dotnet Source/Identity.Contracts.Check/bin/Release/net10.0/Cratis.Identity.Contracts.Check.dll --released-ref "$base_commit" Identity.slnx
```

The same job runs `dotnet build Identity.slnx --configuration Release -warnaserror`
and `dotnet format Identity.slnx --verify-no-changes --no-restore`, matching the
local gates. No follow-up workflow wiring is needed. The trusted base comes from
the event, never from proposed fixture or catalogue content.

The initial `WireName` snapshot preserves reflection names/explicit aliases;
Chronicle's default camelCase serialization is exercised by the fixture round-trip
specs. Normalizing those already recorded names is deliberately not part of this
immutability fix, because it would rewrite retained rows.

## Reviewing a safe concept

A suggestive name or a Hash/Thumbprint/Fingerprint suffix is not an exception.
Add a leaf `ConceptAs<T>` and an exact fully qualified entry in
`SafeConceptRegistry` only after reviewing its serialized primitive, producer
specification, and justification. Record the non-redeemable reference properties,
public-key canonicalization, or digest algorithm, entropy/keying, purpose, and
key revision as appropriate. Evidence describes the writer; the checker does not
hash or sanitize event instances. Password verifiers, bytes, cryptographic
containers, open objects, and arbitrary nested content remain excluded.

The existing upstream lookup entry cites `UpstreamKeyDigester`'s exact UTF-8,
length-framed, domain-separated HMAC and `LookupRevisionGuard`'s revision checks.
It remains PII tied to the account subject. Do not copy these metadata fields
into event payloads or reuse signing, token, or credential-store keys.

## Diagnostics and specs

Diagnostics contain only event identity, generation, visibility, member path,
and a stable reason id. They never inspect or print event values. Examples include
`forbidden_member_name`, `forbidden_payload_type`, `open_payload_shape`,
`unapproved_safe_concept`, `missing_pii`, `unresolved_compliance_subject`, and
`event_fields_mismatch`. Fix the declared shape or review the catalogue; do not
suppress a diagnostic or rename sensitive content to bypass it.

```sh
dotnet test Source/Identity.Specs/Identity.Specs.csproj -c Release --filter FullyQualifiedName~for_EventContentRules
```

The locked content and PII vectors and reflected fixtures use the same pure rule
engine. Runner specs assert nonzero status for violations and empty discovery;
catalogue specs cover identity, visibility, field, protection, and invalid generation
markers. Discovery specs cover registrations in a second assembly, missing production
outputs, and allocated catalogue rows without registrations.

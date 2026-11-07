<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Event content check

The ordinary solution build runs this non-packable executable after compiling
Contracts. A content violation returns exit code 1 and fails MSBuild. No service,
database, Docker container, or secret-store access is needed.

```sh
dotnet build Identity.slnx -c Release -warnaserror
```

For a previously built Release assembly, run the same checker directly:

```sh
dotnet Source/Identity.Contracts.Check/bin/Release/net10.0/Cratis.Identity.Contracts.Check.dll Source/Identity.Contracts.Check/bin/Release/net10.0/Cratis.Identity.Contracts.dll
```

The successful build stamp depends on both compiled assemblies. Changing the
checker, its embedded catalogue, or Contracts invalidates that stamp. Failed
checks never update it. The dependency graph is Contracts → Check → Specs; the
Contracts package does not depend on the executable.

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
catalogue specs cover identity, visibility, field, protection, and generation drift.

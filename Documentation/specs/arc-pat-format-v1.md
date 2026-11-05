# PAT format

```text
Spec id: arc-pat-format
Version: 1
Status: Locked
Locked: 2026-11-05, https://github.com/Cratis/Identity/issues/14
Issue: https://github.com/Cratis/Arc/issues/3699
Sign-off issue: https://github.com/Cratis/Identity/issues/14
Supersedes: None
```

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Purpose

A resource prefix makes PAT dispatch deterministic. The application remains the PAT verifier; dispatch and checksum checks do not authenticate anyone. This specification is not a claim of shipped behavior.

## Terms

Person, Account, Realm, Issuer, Subject, Linked key, Resource, Resource indicator, Audience, Tenant, Organisation, Environment, Actor, Credential kind, Assurance, Policy epoch, Access token, ID token, Refresh token, and PAT have exactly the meanings in the Terms table of [identity-contract-v1.md](identity-contract-v1.md), Cratis/Identity#24. Shared meanings are not redefined here.

A **PAT prefix** is the registered resource key and format marker before a token's random payload. A **checksum** is a transcription check, not authentication. A **PAT verifier** is the application component checking a presented PAT against a stored one-way verifier and its authorization record.

## Normative contract

MUST, MUST NOT, and SHOULD have their [RFC 2119](https://www.rfc-editor.org/rfc/rfc2119) meanings. Requirements apply after sign-off. The precise wire format below is the q11 encoding decided at the Identity #14 sign-off.

### Wire format

```text
<resource-key>_pat_v1_<secret>_<checksum>
```

- `resource-key` MUST be an exact registered ASCII key: `direct`, `studio`, or `stage-<app-key>`. An `app-key` MUST contain 1–48 lowercase ASCII letters, digits, or internal hyphens and MUST start and end with a letter or digit. Keys MUST be unique within the dispatch domain; shared keys MUST denote the same logical resource and verification boundary.
- `secret` MUST be 32 cryptographically random bytes encoded as exactly 64 lowercase hexadecimal characters. Issuance MUST NOT use the fixed test payload below.
- `checksum` MUST be the first eight lowercase hexadecimal characters of SHA-256 over the ASCII bytes preceding the final underscore. The hashed body includes the resource key, `_pat_v1_`, and encoded secret. It excludes the underscore immediately before the checksum.
- Parsing MUST reject incorrect lengths, extra segments, unsupported versions, noncanonical casing, whitespace, and malformed encodings. It MUST NOT normalize invalid input into an accepted credential.
- The checksum MUST NOT substitute for secret verification or resource binding. It detects transcription mistakes, not malicious alteration.

The application MUST store a one-way verifier rather than a recoverable token. Verification MUST bind the token to the stored resource, tenant, person, scopes, expiry, revocation state, and applicable policy epoch. Verification MUST also compare the record's `recoveryEpoch` with the current authority/realm recovery epoch read from independent custody, per [identity-revocation-v1.md](identity-revocation-v1.md) §6. An older, missing, or unavailable epoch MUST cause refusal, with no policy re-evaluation; restore invalidates every PAT. Secret-derived verifier comparison MUST be constant-time. A valid checksum from another resource MUST still be refused.

### Dispatch

The PAT namespace MUST include any credential value beginning with `<key>_pat_` under any Authorization scheme, including Bearer and DPoP. The whole prefix, including `_pat_`, MUST be matched ASCII case-insensitively (`^[a-z0-9-]+_pat_`, case-insensitive); `key` contains no `.`. Namespace recognition MUST precede OAuth verification and MUST NOT imply canonical format validity. Every value in that namespace MUST be handled only by the PAT branch. PAT-namespace values under a non-Bearer scheme, unregistered keys, unsupported versions, wrong casing, and malformed values MUST be refused there; they MUST NOT reach an OAuth verifier or introspection endpoint.

Credentials are the Authorization header value, a session cookie, an AuthProxy identity envelope, and any credential in the URI query. A session cookie plus an Authorization bearer counts as two credentials. An AuthProxy identity envelope plus Authorization MUST count as one credential only when the verified envelope cryptographically covers that exact Authorization value (forwarded identity v2 signs its SHA-256 hash, Cratis/Arc#3698); otherwise they MUST count as two. A DPoP proof accompanying its token is not an additional credential. A request carrying more than one credential, or any credential in the URI query, MUST be refused before dispatch, following RFC 6750 §2 and OAuth 2.1. The dispatcher MUST NOT choose one credential and ignore the others.

Other bearer values MUST go only to the configured OAuth verifier, if enabled. JWT-like shape MUST NOT establish validity: the OAuth branch MUST check signature, issuer, audience, expiry, and applicable sender binding. Values valid in neither branch MUST be refused. The [MCP contract](arc-mcp-authorization-v1.md) specifies the machine response.

AuthProxy MUST route PATs to the application without asserting a verified PAT identity. AuthProxy MUST NOT attach a person envelope (an envelope whose credential kind is not `pat`) to a request it forwards with a PAT. Caller construction when an envelope accompanies a bearer MUST follow [Envelope and bearer together](arc-verified-caller-v1.md#envelope-and-bearer-together). Both paths MUST apply the same product authorization rules.

### Creation and policy

A creation record MUST retain `credentialId`, `accountKey` (`issuer`, `subject`), product `person` reference, `resource`, `environment`, `tenant`, `scopes`, `createdAt`, `expiresAt`, `assurance`, and a required `recoveryEpoch`: the authority/realm recovery epoch current at creation, as defined in [identity-revocation-v1.md](identity-revocation-v1.md) §6 (event field name `RestoreEpoch`). Where organisation policy applies, it MUST also retain `policyEpoch` as a nonnegative integer. Assurance MUST retain verified `idp`, `auth_time`, `acr` (`basic` if unknown), and optional mapped `amr`, as defined in identity-contract-v1. `auth_time` MUST be integer UTC seconds and MUST NOT be refreshed by PAT use. User-submitted assurance MUST NOT substitute for verified context.

Expiry MUST be no later than creation plus 90 days and MUST obey a shorter organisation cap. Organisations MUST be able to forbid PATs. Under D12's recommended default, creation MUST meet the organisation's SSO and authentication freshness policy. Missing required assurance MUST cause refusal. A relevant policy change MUST invalidate the PAT's prior policy authorization and force re-evaluation against current policy; an old epoch MUST NOT establish authority. Current policy may permit continued use only after that re-evaluation.

PAT scopes MUST only narrow current product permissions. Revoked PATs MUST be refused under the agreed revocation contract. Audit records MUST contain credential ids, not tokens or secrets. PAT labels MUST remain declared attribution, not authenticated actor identity.

## Test vectors

The fixtures are synthetic. `Z` is exactly 64 ASCII zero characters and MUST NOT be used for issuance. Define `C(body) = lowercase_hex(SHA256(ASCII(body)))[0:8]` and `T(body) = body + "_" + C(body)`. The expanded fixture values below were computed with SHA-256 (Node `crypto` and `shasum -a 256` agree):

| Resource key | `C(body)` | Full fixture value |
| --- | --- | --- |
| `direct` | `000eb748` | `direct_pat_v1_0000000000000000000000000000000000000000000000000000000000000000_000eb748` |
| `studio` | `fa5d04f9` | `studio_pat_v1_0000000000000000000000000000000000000000000000000000000000000000_fa5d04f9` |
| `stage-atlas` | `38cf83ba` | `stage-atlas_pat_v1_0000000000000000000000000000000000000000000000000000000000000000_38cf83ba` |

The bad-checksum vector is the Direct fixture with checksum `000eb749`.

| Input | Expected result |
| --- | --- |
| `T("direct_pat_v1_" + Z)` at Direct, with an active matching fixture record | Direct PAT branch; checksum and verification pass. |
| `T("studio_pat_v1_" + Z)` at Studio, with an active matching fixture record | Studio PAT branch; checksum and verification pass. |
| `T("stage-atlas_pat_v1_" + Z)` at the registered Atlas app, with an active matching fixture record | Atlas PAT branch; checksum and verification pass. |
| Valid Direct fixture with one checksum digit changed | Refused; no OAuth fallback. |
| Valid Studio fixture at Direct | Refused for resource mismatch. |
| Synthetic `unsigned-jwt-shaped-fixture` (`alg=none`, no signature) | OAuth branch only; refused as unsigned. |
| An unregistered `unknown_pat_v1_` key | Refused; no guessed verifier. |
| Valid checksum, absent or revoked record | Refused; checksum is not authentication. |
| Create `2030-01-01T00:00:00Z`, expire `2030-04-01T00:00:00Z`, 90-day cap | Lifetime check passes, subject to other checks. |
| Same creation, expiry one second later, or a 30-day cap | Refused for lifetime cap. |
| Required SSO assurance missing | Refused. |
| Stale policy epoch; current policy refuses after re-evaluation | Refused; old epoch provides no authority. |
| Stale policy epoch; current policy permits after re-evaluation, all other checks pass | Accepted; old epoch is never used as authority. |
| Record `recoveryEpoch` older than the current recovery epoch (after a restore), all other checks pass | Refused; no policy re-evaluation. |
| Record `recoveryEpoch` missing, or current recovery epoch unavailable from independent custody | Refused. |
| `DIRECT_PAT_v1_` namespace marker, unsupported `direct_pat_v2_`, or malformed `direct_pat_` value | PAT branch only; refused without OAuth verification or introspection. |
| `DPoP` Authorization scheme with a `direct_pat_v1_` value | PAT branch only; refused without OAuth verification or introspection. |
| Session cookie plus Authorization bearer | Two credentials; refused before dispatch. |
| AuthProxy identity envelope plus bearer not cryptographically covered by it | Two credentials; refused before dispatch. |
| Verified `oauth-grant` AuthProxy envelope covering the exact OAuth bearer, matching account key, all other checks pass | One credential; caller constructed only through OAuth verification. |
| Verified non-`pat` envelope covering a `direct_pat_v1_` value | Refused; no caller constructed. |
| Verified `pat`-kind envelope covering a revoked PAT | Refused after local PAT verification; no caller constructed. |
| Verified envelope covering the exact OAuth bearer but with a different signed subject | Refused; no caller constructed. |
| Two credentials on one request, or a credential in the URI query | Refused before dispatch; neither verifier is selected. |

Machine-readable group: [dispatch and policy](../../Source/Identity.Specs/Vectors/arc-pat-format-v1/dispatch-and-policy.json), repository path `Source/Identity.Specs/Vectors/arc-pat-format-v1/dispatch-and-policy.json`. Each JSON group has `name`, `description`, `given`, `when.action`, and `then`; `given.vectors` contains its individual cases. These data vectors do not demonstrate runtime conformance. Outcomes and reasons are vector vocabulary, not public error messages.

## Decisions recorded at sign-off

Decided in Identity #14 (2026-11-05) with the recommended answers from the sign-off package:

- **q11 / D11:** resource-key registration, uniqueness boundaries, checksum algorithm, and version grammar as specified above; per-resource prefixes and exact bytes are decided.
- **D3:** lifecycle for previously issued PATs follows the separately defined announced lifecycle below.
- **D12:** SSO policy inputs, freshness, and invalidation integration as decided at sign-off. Assured, capped creation is the recommended default; organisations can forbid PATs.
- **W0 revocation contract:** revocation timing and policy-epoch enforcement are specified in [identity-revocation-v1](identity-revocation-v1.md).
- **(align with #24):** #24 defines shared identity, assurance (`idp`, `auth_time`, `acr`, optional `amr`), nonnegative policy epochs, and immutable resource/tenant/environment constraints. This spec additionally needs the PAT creation-record field names, one-way verifier scheme, prefix registry, and credential-id allocation; #24 leaves those mechanics to Arc. Consumer review of the shared mappings was covered by the Identity #14 sign-off.
- Legacy tokens follow an announced lifecycle defined separately. This spec does not prescribe that migration.

## Versioning

A Locked spec MUST change only through a new version file. A verifier MUST accept only explicitly supported format versions and MUST NOT reinterpret an unsupported PAT version as OAuth. Prefix or checksum changes MUST use an explicitly versioned format and an announced compatibility lifecycle.

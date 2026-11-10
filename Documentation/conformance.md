# Protocol conformance

Identity is an OAuth authorization server for resource access, **not** an OpenID Connect sign-in provider. The executable catalogue lives in `Source/Identity.Integration.Specs/Conformance/Requirements.cs`. It traces our supported profile to RFC 9700 (OAuth security BCP), RFC 8252 (native apps), RFC 7636 (PKCE), RFC 8707 (resource indicators), RFC 9068 (JWT access tokens), RFC 8414 (AS metadata), RFC 9728 (protected-resource metadata), RFC 9207 (issuer response), RFC 7009 (revocation), RFC 7662 (introspection), and the applicable RFC 6749/7517/7518 requirements. This is a focused regression contract, not a claim of complete RFC certification.

## Catalogue

Spec paths below are relative to `Source/Identity.Integration.Specs/`; names omit `.cs`. **22 pass obligations, 1 expected failure (23 total).** “Pass” means a runnable proof is present and expected to pass, not that it was executed locally. Integration and conformance execute **only in CI**. `RequirementAttribute` emits both `Requirement=<ID>` and `Category=Conformance` xUnit v3 traits, so existing proofs are reused rather than copied. The full unit + integration + conformance CI run remains the acceptance check.

Levels describe the supported profile. Exact single-audience selection and IP-literal loopback redirects include stricter issuer policy; the cited RFCs explain their basis. Key overlap is an operational SHOULD policy, not a claim that RFC 8414 mandates a particular overlap duration.

| Requirement | Reference / level | Obligation | Executable proof | Status |
|---|---|---|---|---|
| PKCE-REQUIRED | RFC 9700 §2.1.1; RFC 7636 §4.4.1 / MUST | Require PKCE for public code clients | `for_AuthorizationRequests/when_pkce_is_missing` | Pass |
| PKCE-S256-ONLY | RFC 7636 §4.2; RFC 9700 §2.1.1 / MUST | Accept S256, not plain | `for_AuthorizationRequests/when_the_plain_pkce_method_is_requested`; `for_Discovery/when_reading_the_authorization_server_metadata` | Pass |
| PKCE-VERIFIER-BOUND | RFC 7636 §4.6 / MUST | Wrong verifier cannot redeem code | `for_CliLogin/when_redeeming_a_code_with_the_wrong_verifier` | Pass |
| REDIRECT-EXACT-LOOPBACK | RFC 8252 §7.3, §8.4; RFC 9700 §2.1 / MUST + policy | Exact redirect, native IP-literal loopback port exception | `for_AuthorizationRequests/when_the_redirect_uses_localhost`; `for_CliLogin/when_logging_in_with_pkce_on_an_arbitrary_loopback_port`; `for_CliLogin/when_logging_in_on_an_ipv6_loopback_port` | Pass |
| STATE-AND-ISS-IN-RESPONSE | RFC 6749 §4.1.2; RFC 9207 §2 / MUST | Echo state and identify issuer | `for_CliLogin/when_logging_in_with_pkce_on_an_arbitrary_loopback_port` | Pass |
| ACCESS-TOKEN-AUDIENCE | RFC 9068 §2.2, §4; RFC 8707 §2 / MUST | Signed at+jwt, issuer, audience, claims | `for_CliLogin/when_logging_in_with_pkce_on_an_arbitrary_loopback_port` | Pass |
| RESOURCE-UNKNOWN-REFUSED | RFC 8707 §2 / MUST | Unknown resource yields invalid_target | `for_AuthorizationRequests/when_the_resource_is_unknown` | Pass |
| RESOURCE-SINGLE-AUDIENCE | RFC 8707 §2; issuer policy / MUST | No mixed audience or resource switch at redemption | `for_AuthorizationRequests/when_resources_of_two_audiences_are_requested`; `for_CliLogin/when_redeeming_a_code_for_another_resource` | Pass |
| REFRESH-ROTATION | RFC 9700 §4.14.2 / MUST | Rotate refresh tokens | `for_RefreshTokens/when_a_rotated_refresh_token_is_reused` | Pass |
| REFRESH-REUSE-REVOKES | RFC 9700 §4.14.2 / MUST | Replay revokes active grant, including races | All three specs in `for_RefreshTokens/` | Pass |
| REVOCATION-REVOKES-GRANT | RFC 7009 §2.1 / SHOULD | Revoke access and refresh tokens with grant | `for_Revocation/when_the_cli_revokes_its_refresh_token` | Pass |
| INTROSPECTION-INACTIVE-AFTER-REVOKE | RFC 7662 §2.2 / MUST | Revoked access token is inactive | `for_Revocation/when_the_cli_revokes_its_refresh_token` | Pass |
| INTROSPECTION-AUTHENTICATED | RFC 7662 §2.1 / MUST | Reject anonymous and wrongly authenticated callers | `for_Introspection/when_a_client_introspects_an_access_token` | Pass |
| AS-METADATA | RFC 8414 §2, §3.3 / MUST | Issuer, endpoints, scopes, code-only supported profile | `for_Discovery/when_reading_the_authorization_server_metadata` | Pass |
| JWKS-PUBLIC-ONLY | RFC 7517 §5; RFC 7518 §6 / MUST | Public signing key; no RSA/EC private or symmetric parameters | `for_Discovery/when_reading_the_authorization_server_metadata` | Pass |
| CODE-SINGLE-USE | RFC 6749 §4.1.2 / MUST | First redemption succeeds; replay is invalid_grant | `for_Conformance/when_redeeming_the_same_authorization_code_twice` | Pass |
| CODE-REDIRECT-BOUND | RFC 6749 §4.1.3 / MUST | Even a changed loopback port is invalid at redemption | `for_Conformance/when_redeeming_a_code_with_a_different_redirect` | Pass |
| NO-IMPLICIT | RFC 9700 §2.1.2 / SHOULD | Reject response_type=token | `for_Conformance/when_requesting_an_implicit_token` | Pass |
| NO-PASSWORD-GRANT | RFC 9700 §2.4 / MUST | Reject password grant | `for_Conformance/when_requesting_a_password_grant` | Pass |
| TOKEN-RESPONSE-NO-STORE | RFC 6749 §5.1 / MUST | Cache-Control: no-store, Pragma: no-cache, Bearer, positive expiry | `for_Conformance/when_reading_a_successful_token_response` | Pass |
| REVOCATION-UNKNOWN-TOKEN | RFC 7009 §2.2 / MUST | Unknown token still returns HTTP 200 | `for_Conformance/when_revoking_an_unknown_token` | Pass |
| AS-METADATA-FROM-PRM | RFC 9728 §3; RFC 8414 §3.1, §3.3 / MUST | Resource challenge → PRM → issuer → AS metadata → JWKS → PKCE → audience validation | `for_Conformance/when_discovering_identity_from_a_protected_resource` | Pass |
| KEY-ROTATION-OVERLAP | RFC 8414 §2 (jwks_uri); operational policy / SHOULD | Retain both signing keys during rotation overlap | `for_Conformance/when_rotating_signing_keys` | Pass |

The PRM proof starts with an unauthenticated request to a minimal Kestrel resource double. Its challenge identifies the metadata URL. That metadata advertises the registered canonical Direct MCP resource and Identity issuer; the client discovers AS endpoints and JWKS, completes real code + S256 consent and redemption, and sends the resulting token to the double. It validates signature, issuer, `at+jwt` type and `direct-api` audience, and rejects a genuine same-issuer Studio token. Loopback HTTP is only the double's test transport; it does not claim to implement Direct/MCP or register a new audience.

## Expected failures and catalogue checks

`ExpectedFailures.cs` is the only accepted gap list. Each entry needs a known ID, a reason and a follow-up issue. The list is currently empty: key overlap is proven by `for_Conformance/when_rotating_signing_keys`, which publishes, overlaps and retires keys in the secret store while Identity runs.

For runnable assertions, `Conformance.Expect` runs unlisted assertions normally. A listed assertion that throws produces a dynamic skip with its reason/issue. A listed assertion that succeeds fails the run, requiring removal from the gap list: stale expected failures cannot remain green. Never add blanket `[Fact(Skip=...)]`. Catalogue meta-specs discover declared instance facts (public and private, including Theory and other derived Fact attributes), require a non-empty tagged fact set and a mapping for every catalogue entry, check known and unique IDs, tagged classes with facts, reasons/issues, absence of static skips, and prohibit MUST-level expected failures. Base classes are contexts; proof facts must be declared on their tagged spec. Focused regression meta-specs reject missing mappings, unknown mapped requirements and empty discovery. Security MUST failures (PKCE, redirect, audience, replay revocation and introspection authentication) fail normally; they are not gap-list candidates.

## Device authorization profile

Device authorization is opt-in through `Identity:Device:Enabled`; the integration fixture explicitly enables it. The shared fixture uses higher bounded account and IP limits so independent scenarios can share one account and peer. Unit specs cover default bounds, shared limit exhaustion, failure-closed admission and logical expiry. Deliberate Chronicle outages use a separate host so their recovery state cannot affect ordinary protocol scenarios.

Device authorization errors return uncacheable OAuth JSON, including when admission is unavailable or a requested resource is invalid. `for_DeviceAuthorizationError/when_returning_a_protocol_error` verifies the response envelope without an external host.

| Id | Source | Obligation | Executable proof |
| --- | --- | --- | --- |
| `DEVICE-LIFECYCLE` | RFC 8628 §3.2, §3.5 | Bounded codes, manual verification URI and explicit approval before token issuance. | `for_DeviceGrant/when_a_user_approves_a_device` |
| `DEVICE-POLLING` | RFC 8628 §3.5 | Pending requests issue no tokens; early polling increases the interval. | `for_DeviceGrant/when_a_device_polls_before_approval` |
| `DEVICE-DENIAL` | RFC 8628 §3.5 | User denial refuses token issuance. | `for_DeviceGrant/when_a_user_denies_a_device` |
| `DEVICE-SINGLE-USE` | RFC 8628 §3.5 | A redeemed device code cannot be exchanged again. | `for_DeviceGrant/when_a_redeemed_device_code_is_reused` |
| `DEVICE-BINDING` | RFC 8628 §3.5 | Only the initiating client can redeem a device code. | `for_DeviceGrant/when_another_client_redeems_a_device_code` |

Rate keys use purpose-separated SHA-256. Guess and issuance counters use atomic MongoDB increment/upsert; polling retains compare-and-swap to preserve the cumulative five-second slowdown and deadline together. TTL cleanup is not expiry enforcement. Verification also checks antiforgery, session-bound confirmation, live account and tenant membership, independent recovery epochs and grant fences. Durable consent events are outside this profile.

## Opt-in issuer-side DPoP

DPoP is disabled by default. AuthProxy remains Bearer-only; resource-server proofs (`ath`), edge forwarding, organization policy, token exchange and `client_credentials` are deferred. No certification claim is made. The new integration scenarios below were added for CI and were **not run locally**.

| Requirement | Reference | Behavior | Proof |
| --- | --- | --- | --- |
| `DPOP-BOUND-ISSUANCE` | RFC 9449 §5 and §6 | Code and approved device issuance emit object-valued `cnf.jkt` and `token_type=DPoP`. | `for_DPoP/when_using_sender_bound_grants` |
| `DPOP-REFRESH-BINDING` | RFC 9449 §5 | Preserve operational family binding; refuse missing/wrong keys without consumption; refuse attaching a key to an unbound family. | `for_DPoP/when_using_sender_bound_grants` |
| `DPOP-AS-NONCE` | RFC 9449 §8 | Stateless nonce challenges do not consume the code. | `for_DPoP/when_using_sender_bound_grants` |
| `DPOP-PROOF-REPLAY` | RFC 9449 §4.3 | Shared reservations reject replay across replicas, concurrent requests and logical expiry before TTL cleanup. | `for_DPoP/when_using_sender_bound_grants`, `for_DPoP/when_reserving_across_replicas` |
| `DPOP-PROOF-VALIDATION` | RFC 9449 §4.3 | Validate against the exact configured public endpoint. | `for_DPoP/when_using_sender_bound_grants`; focused unit specs also check bounded ES256 parsing and signatures. |
| `DPOP-METADATA` | RFC 9449 §5.1 | Advertise ES256 only when enabled. | `for_DPoP/when_using_sender_bound_grants` |

The AS nonce uses the issuer's existing shared Data Protection ring and purpose `Cratis.Identity.Dpop.Nonce.v1`, replacing the draft profile's standalone HMAC secret/encoding. The secret-store loader and contracts are unchanged. The older fixed nonce-encoding fixture describes the draft HMAC format, not Data Protection ciphertext. Focused unit specs cover nonce origin/key/time binding, shared-store failure, immutable family properties and access-only confirmation. Retained ring keys remain usable across rotation; all replicas must share the ring and application discriminator.

## Running and upgrading

Local gate (never run the integration executable locally):

```sh
dotnet build Identity.slnx --configuration Release
./Source/Identity.Specs/bin/Release/net11.0/Cratis.Identity.Specs
actionlint .github/workflows/*.yml # if installed
```

CI separates these steps without duplicating tagged proofs:

```sh
# Integration specs
./Source/Identity.Integration.Specs/bin/Release/net11.0/Cratis.Identity.Integration.Specs -notrait "Category=Conformance"
# Protocol conformance (RFCs)
./Source/Identity.Integration.Specs/bin/Release/net11.0/Cratis.Identity.Integration.Specs -trait "Category=Conformance"
```

OpenIddict is centrally pinned to **7.7.1** in `Directory.Packages.props`; this change does not bump it. Dependabot checks NuGet monthly, allows only `OpenIddict*`, groups those updates and opens at most one PR. There is no automatic merge/publish path.

**Ownership:** Identity module maintainers own OpenIddict upgrades. The dependency PR author performs the checklist; a reviewing Identity maintainer owns the protocol/security decision and confirms the complete CI result before accepting it. Dependabot and the candidate probe propose evidence, not approval.

Before accepting an upgrade:

1. Read upstream release notes, security advisories and migration notes; inspect changes to handlers, validation order, token storage and public metadata.
2. Dispatch **OpenIddict compatibility** manually. Optional repository variable `OPENIDDICT_CANDIDATE_VERSION` selects an exact candidate (including prereleases); when unset it resolves the latest stable NuGet version. The job changes only its disposable checkout, builds Release, runs unit specs and the conformance trait. It does not commit, push, publish or update the repository pin. It is intentionally unscheduled and is not a required branch check.
3. Review every protocol result and every dynamic skip. No new MUST-level failure is an acceptable expected failure. Investigate unexpected success and remove its gap entry. Do not regenerate an opaque discovery/JWKS snapshot to hide a contract change.
4. Update the two central OpenIddict pins together in the actual dependency PR (normally the grouped Dependabot PR). Run the local gate; require green Build and specs CI, including ordinary integration and conformance. The candidate probe alone does not replace that complete gate.
5. Update this catalogue if supported behavior intentionally changes. Name its normative reference, proof and any explicitly owned non-MUST gap in review.

Scope deliberately excludes OIDC provider behavior, logout protocols, resource-server DPoP, JAR/PAR, upstream login changes and production key management. Changes to those profiles need their own reviewed requirements.

## Client compatibility

How each MCP client registers, discovers and redirects is recorded in the [MCP client compatibility matrix](design/mcp-client-compatibility-matrix.md). It is documented behavior, not a tested claim.

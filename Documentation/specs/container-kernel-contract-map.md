<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Container and kernel contract map

This map records what Identity's container-backed assertions protect. It is a
coverage inventory, not a record of failures caught: no historical detection rate
has been measured. **Every existing assertion is kept. None is retired or replaced.**

## Reading the map

The source root below is
[`Source/Identity.Integration.Specs`](../../Source/Identity.Integration.Specs).
A suite row selects **every assertion in every fact and theory in that directory**,
including all inline-data cases and assertions in called helpers. The scenario
rows refine that mapping; the named-method rows add platform candidates. These
are universal selectors, not examples: an assertion inherits its suite's contract
and any matching scenario or method row. No assertion is unclassified merely
because its method name is not repeated here.

| Classification | Contract protected |
| --- | --- |
| Registration vs kernel version | Identity's artifact, command, constraint, projection and realm registration works against the configured real Chronicle kernel. This is not just a framework API compiling. |
| Security | Identity's authorization, isolation, credential lifecycle, privacy or fail-closed policy, including the configuration that enables it. |
| Migration | Existing stores, revision changes, schema baselines and recovery state remain safe to open or upgrade. |
| Pinned-version | Behavior with the dependencies in `Directory.Packages.props` and the images selected by the fixtures. A candidate dependency run is separate evidence, not proof for the pin. |
| Deploy image | The built `cratis-id` Docker target starts, becomes ready and refuses unsafe configuration. |
| Platform behavior (Arc/Chronicle) | A potentially upstream-owned primitive, such as keyed append, concurrency or projection progress. **Candidate** means keep it until a specific upstream repository/path and assertion prove equivalent coverage. |

Multiple classifications are intentional. A security assertion can also expose a
kernel registration or pinned-version incompatibility. A candidate annotation does
not weaken either contract and is not permission to remove the assertion.

## Fixtures and version boundary

- [`IdentityHost`](../../Source/Identity.Integration.Specs/given/IdentityHost.cs)
  starts MongoDB (`mongo:8.0.14`) and an independent fence-log MongoDB replica set,
  then a real Chronicle kernel. It hosts Identity in-process; upstream sign-in and
  tenant callbacks are stubs, not live providers.
- [`ChronicleKernel`](../../Source/Identity.Integration.Specs/given/ChronicleKernel.cs)
  selects `IDENTITY_CHRONICLE_IMAGE`, defaulting to `cratis/chronicle:19.144.0-development`.
  [`Directory.Packages.props`](../../Directory.Packages.props) pins the Chronicle
  client to `19.144.0`, Arc to `22.185.0` and OpenIddict to `7.8.0`.
  The pin is a version-tag contract, not an image-digest guarantee. Changing a pin
  or overriding the image requires retaining the registration checks below.
- Development, OIDC, DPoP, metadata-client, exchange and key-rotation hosts extend
  that real-store boundary with their own profiles and collections. The outage
  collection deliberately interrupts it. Grant-fence and legacy-store scenarios
  also exercise MongoDB directly; they are not Arc/Chronicle re-tests.
- [`CratisIdImageFixture`](../../Source/Identity.Integration.Specs/for_Host/given/CratisIdImageFixture.cs)
  exercises the locally built image, separately from the in-process host. Its
  facts require `CI=true` and `CRATIS_ID_IMAGE_SMOKE_ENABLED=true`.
- `Identity.Specs` does not start Testcontainers or a real kernel. The conformance
  catalog checks below are container-free, but remain in the integration assembly
  and its existing CI lane. This inventory does not move or duplicate them.

## Suite-to-assertion coverage

Each linked suite selector covers all of its assertions. The following sections
name every scenario and explain the more specific contract. All rows are **kept**.

| Suite selector (all assertion methods) | Classification | Protected boundary |
| --- | --- | --- |
| [`for_AccountErasure/*`](../../Source/Identity.Integration.Specs/for_AccountErasure) | Security; registration vs kernel version; pinned-version | Erasure event registration, resumable completion, keyed requests, key destruction and permanent tombstones. |
| [`for_AuthorizationRequests/*`](../../Source/Identity.Integration.Specs/for_AuthorizationRequests) | Security; pinned-version | Consent, CSRF, tenant membership, redirect/resource/PKCE admission and safe upstream errors. |
| [`for_CliLogin/*`](../../Source/Identity.Integration.Specs/for_CliLogin) | Security; pinned-version | CLI loopback/PKCE flow and Identity's subject, audience, tenant, presenter and scope claims. |
| [`for_ClientIdMetadataDocuments/*`](../../Source/Identity.Integration.Specs/for_ClientIdMetadataDocuments) | Security; pinned-version | Public metadata-client discovery, admission, refresh and provider/cache isolation. |
| [`for_Conformance/*`](../../Source/Identity.Integration.Specs/for_Conformance) | Security; pinned-version | Identity's supported OAuth profile, resource discovery and operational signing-key overlap. |
| [`for_ConformanceCatalog/*`](../../Source/Identity.Integration.Specs/for_ConformanceCatalog) | Security; pinned-version | Executable requirement coverage cannot be empty, unknown, duplicated or hidden by skips/expected failures. |
| [`for_DPoP/*`](../../Source/Identity.Integration.Specs/for_DPoP) | Security; pinned-version | Proof binding, shared replay reservations, nonce challenges, rotation and enabled-profile metadata. |
| [`for_DevelopmentProfile/*`](../../Source/Identity.Integration.Specs/for_DevelopmentProfile) | Security; pinned-version | Local-only issuer/key/audience boundary and narrowly scoped localhost redirects. |
| [`for_DeviceGrant/*`](../../Source/Identity.Integration.Specs/for_DeviceGrant) | Security; pinned-version | Explicit approval, request binding, CSRF, polling, replay and non-disclosing verification UI. |
| [`for_Discovery/*`](../../Source/Identity.Integration.Specs/for_Discovery) | Security; pinned-version | Exact enabled OAuth surface and public-only JWKS. |
| [`for_GrantFences/*`](../../Source/Identity.Integration.Specs/for_GrantFences) | Security; migration; pinned-version | Independent durable revocation custody, restore epochs, ordered reservations and rollback-safe progress. |
| [`for_Host/*`](../../Source/Identity.Integration.Specs/for_Host) | Security; pinned-version | Host liveness and response hardening; image facts additionally protect the deploy image. |
| [`for_Introspection/*`](../../Source/Identity.Integration.Specs/for_Introspection) | Security; pinned-version | Authenticated introspection, stable grant identity, tenant/scope/epoch shape and inactive-only refusals. |
| [`for_LegacyAccountStoreGuard/*`](../../Source/Identity.Integration.Specs/for_LegacyAccountStoreGuard) | Migration; security; pinned-version | Legacy-store startup guard, operator confirmation and bounded, non-disclosing probe failure. |
| [`for_LookupRevisionGuard/*`](../../Source/Identity.Integration.Specs/for_LookupRevisionGuard) | Migration; security; pinned-version | Mixed lookup-key revisions cannot both start. |
| [`for_McpAcceptance/*`](../../Source/Identity.Integration.Specs/for_McpAcceptance) | Security; pinned-version | Separate product grants and resource audiences; explicit gaps for real edge/tool enforcement. |
| [`for_NativeAccounts/*`](../../Source/Identity.Integration.Specs/for_NativeAccounts) | Registration vs kernel version; security; migration; pinned-version | Identity realm registration, account events, constraints, progress gates, bounded retry and absence of legacy writes. |
| [`for_Oidc/*`](../../Source/Identity.Integration.Specs/for_Oidc) | Security; pinned-version | Opt-in confidential-client OIDC profile, ID-token claims/signatures, userinfo and grant fencing. |
| [`for_RefreshTokens/*`](../../Source/Identity.Integration.Specs/for_RefreshTokens) | Security; pinned-version | Single-use rotation and revocation ordering across refresh races. |
| [`for_Revocation/*`](../../Source/Identity.Integration.Specs/for_Revocation) | Security; pinned-version | Revocation denial authority is independent of Chronicle/projection availability; source-fact replay also protects registration vs kernel version. |
| [`for_Sessions/*`](../../Source/Identity.Integration.Specs/for_Sessions) | Security; pinned-version | Session identifiers, cookie protection, logout CSRF and `max_age` enforcement. |
| [`for_TenantsCallback/*`](../../Source/Identity.Integration.Specs/for_TenantsCallback) | Security; pinned-version | Tenant choices and refresh admission use the callback and fail closed during outages. |
| [`for_TokenExchange/*`](../../Source/Identity.Integration.Specs/for_TokenExchange) | Security; pinned-version | External subject/tenant/resource binding, refusal validation and shared replay protection. |
| [`for_TokenRateLimiting/*`](../../Source/Identity.Integration.Specs/for_TokenRateLimiting) | Security; pinned-version | Token-endpoint limiter precedes client authentication and recovers after the full window. |
| [`for_TokenStorage/*`](../../Source/Identity.Integration.Specs/for_TokenStorage) | Security; pinned-version | Expired stored codes/refresh tokens are refused; refresh expiry is not reuse; token TTL index is installed. |

## Scenario refinements

Scenario names are source filenames without `.cs`, relative to the linked suite.
`All` means every assertion method and every assertion within it, not just the
HTTP status assertion. For parameterized methods it includes every data row.

### Kernel-backed account contracts

| Suite / scenario | Assertion scope | Specific protection |
| --- | --- | --- |
| `for_NativeAccounts/when_signing_in_through_the_native_pipeline` | All | Immutable subject; internal and public registration in the realm; caught-up profile; no tenant account facts/materialization or legacy collection. |
| `for_NativeAccounts/when_a_tenant_request_completes_the_github_callback` | All | Real OAuth callback reads the realm account even when a tenant namespace is supplied. |
| `for_NativeAccounts/when_tenant_requests_use_the_identity_realm` | All | Session, token and Mine stay realm-owned; unrelated host work remains tenant-owned; anonymous Mine creates neither session nor account facts. |
| `for_NativeAccounts/when_posting_the_protocol_command_over_http` | All | Protocol-only command is refused before native handlers or event writes for both authentication and validation modes. |
| `for_NativeAccounts/when_projection_progress_is_held` | All | Account command waits for its registered projection and returns the caught-up Identity profile after release. |
| `for_NativeAccounts/when_projection_progress_times_out` | All | Timeout is `temporarily_unavailable`, with no session account or timed-out decision events. |
| `for_NativeAccounts/when_two_first_sign_ins_race` | All | Real uniqueness/concurrency rejection; one retry after catch-up; winning reservation/subject only; one registration and permanent owner; no losing-subject events. |
| `for_NativeAccounts/when_an_account_is_disabled_after_the_sign_in_recheck` | All | Stale decision conflicts; fresh bounded retry observes disabled state; no session or stale profile writes. |
| `for_NativeAccounts/when_an_account_is_tombstoned_after_the_sign_in_recheck` | All | Retry preserves the original reservation/subject but refuses a tombstoned account; only the competing tombstone commits. |
| `for_NativeAccounts/when_an_upstream_key_is_unlinked_after_the_sign_in_recheck` | All | Retry observes inactive reservation and removed link, preserves the active account, refuses the key and appends no stale sign-in changes. |
| `for_NativeAccounts/when_both_sign_in_attempts_lose_optimistic_concurrency` | All | Two real conflicts stop after one retry; no account/session or stale decisions commit. |
| `for_AccountErasure/when_erasing_a_tombstoned_account_in_the_real_kernel` | All | Request and completion recorded once across reruns; boundary version retained; no failure; completed fold; names unreadable while original stream events and tombstone remain. |
| `for_AccountErasure/when_the_erasure_log_is_appended_twice_in_the_real_kernel` | All | Keyed request deduplication; changed content becomes `AccountOperationRefused`; first failure is retained. |
| `for_Revocation/when_replaying_source_facts_in_the_real_kernel` | All | Real keyed-append capability; one fact across fresh writers; changed content becomes `OutboxContentConflict`. |

### Authorization and client profiles

| Suite / scenario | Assertion scope | Specific protection |
| --- | --- | --- |
| `for_AuthorizationRequests/when_a_selectable_tenant_is_hinted` | All | Preselection does not remove other choices; the consent-selected tenant binds the grant. |
| `for_AuthorizationRequests/when_an_unselectable_tenant_is_hinted` | All | No membership means no preselection; selectable tenants remain available. |
| `for_AuthorizationRequests/when_a_tenant_outside_the_selectable_list_is_submitted` | All | No code; consent asks again rather than accepting a forged tenant. |
| `for_AuthorizationRequests/when_the_account_has_no_tenant_for_the_resource` | All | `access_denied` to the validated client with no code. |
| `for_AuthorizationRequests/when_resources_of_two_audiences_are_requested` | All | `invalid_target` and no code for mixed audiences. |
| `for_AuthorizationRequests/when_the_resource_is_unknown` | All | `invalid_target`, bad request and no client redirect. |
| `for_AuthorizationRequests/when_the_redirect_uses_localhost` | All | Deployed-profile localhost redirect refused without redirecting anywhere. |
| `for_AuthorizationRequests/when_pkce_is_missing` | All | Missing PKCE is `invalid_request`; no redirect. |
| `for_AuthorizationRequests/when_the_plain_pkce_method_is_requested` | All | Plain PKCE is `invalid_request`; no redirect. |
| `for_AuthorizationRequests/when_consent_is_submitted_without_the_antiforgery_token` | All | Forged consent is refused without a code redirect. |
| `for_AuthorizationRequests/when_the_user_denies_consent` | All | `access_denied` and no code. |
| `for_AuthorizationRequests/when_no_session_exists_and_prompt_is_none` | All | `login_required` and no code. |
| `for_AuthorizationRequests/when_github_sign_in_is_cancelled` | All | Validated client receives safe `access_denied`; invalid challenge state gets a generic error; no code or provider-error disclosure. |
| `for_CliLogin/when_logging_in_with_pkce_on_an_arbitrary_loopback_port` | All | Consent names client, redirect and resource; callback preserves port/state/issuer; valid access/refresh tokens carry canonical subject, upstream identity, tenant, authorized party and scope; introspection is active. |
| `for_CliLogin/when_logging_in_on_an_ipv6_loopback_port` | All | Exact IPv6 callback and redemption redirect work. |
| `for_CliLogin/when_redeeming_a_code_for_another_resource` | All | Resource substitution is `invalid_target`. |
| `for_CliLogin/when_redeeming_a_code_with_the_wrong_verifier` | All | Wrong verifier is `invalid_grant`. |
| `for_ClientIdMetadataDocuments/when_using_public_metadata_clients` | All | Opt-in metadata discovery without dynamic registration; authorization/rotation/refresh across fresh providers; repaired documents are usable; PKCE and resource/scope rules still apply; noncacheable documents fetched per request. |
| `for_DevelopmentProfile/when_a_profile_client_signs_in` | All | Consent precedes issuance; local callback works; no ID token; local issuer/key/MCP audience validates, deployed issuer, REST audience and unrelated keys do not. |
| `for_DevelopmentProfile/when_a_redirect_uses_localhost_under_the_profile` | All | Only profile clients may vary loopback ports, never paths; ordinary clients still cannot use localhost. |
| `for_Discovery/when_reading_the_authorization_server_metadata` | All | Exact issuer/endpoints, issuer response parameter, S256, enabled device flow, resource scopes, code-only responses and no implicit OIDC/metadata-client opt-in; all private JWK parameters absent. |

### Device and sender-bound grants

| Suite / scenario | Assertion scope | Specific protection |
| --- | --- | --- |
| `for_DeviceGrant/when_a_user_approves_a_device` | All | Manual-only verification URI, bounded lifetime/poll interval, client/scope/issuer/upstream warnings, active access token and offline refresh token after approval. |
| `for_DeviceGrant/when_a_device_polls_before_approval` | All | Pending and cross-connection slow-down responses never contain access tokens. |
| `for_DeviceGrant/when_a_device_request_is_not_admissible` | All | Unknown client/resource/scope or empty resource scope cannot obtain a device code. |
| `for_DeviceGrant/when_a_verification_code_is_invalid` | All | Unknown/malformed codes yield the same safe message, no reflected code, no storage and no referrer leakage. |
| `for_DeviceGrant/when_csrf_evidence_is_missing` | All | Forged confirmation leaves the device pending and issues no token. |
| `for_DeviceGrant/when_confirmation_evidence_is_missing` | All | Confirmation requires the displayed request binding; device remains pending. |
| `for_DeviceGrant/when_a_user_denies_a_device` | All | Explicit denial returns `access_denied`, without tokens. |
| `for_DeviceGrant/when_another_client_redeems_a_device_code` | All | Wrong registered client receives no token and cannot consume the original client's code. |
| `for_DeviceGrant/when_a_redeemed_device_code_is_reused` | All | Replay issues no new token and revokes the original grant. |
| `for_DPoP/when_using_sender_bound_grants` | All | Nonce challenge preserves code; issued access/refresh tokens share the proof key; rotation preserves binding; missing/wrong/replayed proof cannot consume successor; existing bearer family cannot be rebound; configured public endpoint only; approved device poll binds; ES256 discovery only on enabled resource lane. |
| `for_DPoP/when_reserving_across_replicas` | All | Replay reservation has one winner across replicas, including expired-document replacement before TTL cleanup. |

### Revocation, recovery and token storage

| Suite / scenario | Assertion scope | Specific protection |
| --- | --- | --- |
| `for_GrantFences/when_the_host_provisions_independent_custody` | All | Fence custody uses a distinct database and replica-set identity, matches configured custody identity and passes the production boundary check. |
| `for_GrantFences/when_reappending_the_independent_log` | All | Same identity returns the original sequence across adapters; changed content conflicts. This is Identity's MongoDB fence log, not Chronicle append. |
| `for_GrantFences/when_a_restore_identity_is_reused` | All | Live retry allocates epoch once; changed kind, different host or completed declaration conflicts. |
| `for_GrantFences/when_revocation_precedes_a_stale_redemption` | All | Other instance is fenced; no successor; fence survives store recreation; all outbox intents accompany restriction. |
| `for_GrantFences/when_redemption_precedes_revocation` | All | One successor can install first but its later use on another instance is fenced; no admitted entry remains. |
| `for_GrantFences/when_reading_a_watermark_snapshot` | All | Counter/entries remain consistent through interleaved reservation; stable corruption is refused. |
| `for_GrantFences/when_voiding_holes_before_a_filled_tail` | All | Reserved hole blocks progress; proven void and filled tail advance it; tail tombstone persists. |
| `for_GrantFences/when_the_writer_wins_cancellation` | All | Committed fence fills rather than voids the reservation and advances the proven prefix. |
| `for_GrantFences/when_cancellation_wins_before_the_writer` | All | Operational commit is refused and no fence is installed. |
| `for_GrantFences/when_a_delayed_writer_arrives_after_void` | All | Delayed commit is refused; reservation stays void; only voided prefix advances. |
| `for_GrantFences/when_progress_is_saved_after_an_operational_rollback` | All | Lost checkpoint cannot authorize update/upsert; restore evidence survives. |
| `for_RefreshTokens/when_a_rotated_refresh_token_is_reused` | All | Rotation creates a new token; reuse is `invalid_grant` and fences successor refresh/access tokens. |
| `for_RefreshTokens/when_the_same_refresh_token_is_used_concurrently` | All | Exactly one winner; loser is `invalid_grant`; original and winning successor are fenced. |
| `for_RefreshTokens/when_a_redeemed_refresh_token_is_reused_before_its_successor_is_issued` | All | Reuse is refused even before successor issuance finishes; winner can finish but successor is unusable. |
| `for_RefreshTokens/when_revocation_wins_before_successor_generation` | All | All fifty restrictions acknowledged; every later refresh is refused without access or refresh successor. |
| `for_RefreshTokens/when_refresh_wins_before_revocation` | All | Already admitted refresh may finish; consumed token and successor cannot be used after fence. |
| `for_Revocation/when_the_cli_revokes_its_refresh_token` | All | Revocation acknowledged; refresh is `invalid_grant`; grant access token becomes inactive. |
| `for_Revocation/when_a_revoked_token_is_introspected_again` | All | Active-before/inactive-after lifecycle; denial source outage never yields active. |
| `for_Revocation/when_chronicle_is_unavailable` | All | Revocation acknowledgement and online refresh denial do not require Chronicle. |
| `for_Revocation/when_projection_progress_is_held` | All | Revocation and denial work without projection progress; projection is never denial authority. |
| `for_Revocation/when_a_revoked_token_calls_a_resource_server_mcp_route` | All | Real edge revocation enforcement; retained conditional gap, not evidence of an executed edge check. |
| `for_TokenStorage/when_a_refresh_token_entry_expired_in_the_store` | All | Expired entry is `invalid_grant`, but expiry alone does not revoke the live grant as reuse. |
| `for_TokenStorage/when_an_authorization_code_entry_expired_in_the_store` | All | Expired stored authorization code is `invalid_grant`. |
| `for_TokenStorage/when_reading_the_token_indexes` | All | Expiration-date TTL index uses zero delay; retention configuration is Identity-owned. |

### OIDC and protocol conformance

| Suite / scenario | Assertion scope | Specific protection |
| --- | --- | --- |
| `for_Oidc/when_a_confidential_client_receives_an_id_token` | All | OpenID only for admitted confidential client and allowed scope ceiling; no resource mixing/tenant grant; actual ID token and bearer response. |
| `for_Oidc/when_authenticating_the_confidential_client` | All | Wrong/missing secret refused; correct client still redeems after failed attempts. |
| `for_Oidc/when_discovering_the_provider` | All | Exact issuer/endpoints, RS256, code-only/public subjects, S256 and bounded scopes; no email/upstream identity advertisement. |
| `for_Oidc/when_discovery_is_disabled` | All | Only issuer/keys remain; OAuth does not gain OpenID scopes; userinfo is unmapped. |
| `for_Oidc/when_nonce_is_required_and_echoed` | All | Original nonce echoed; missing nonce refused before login. |
| `for_Oidc/when_prompt_none_is_requested` | All | Consent/login required where appropriate; `none` cannot combine with `login`. |
| `for_Oidc/when_reading_the_id_token_claims` | All | Exact issuer, client audience/authorized party, integer auth time, five-minute lifetime, bounded profile claim set and access-token hash. |
| `for_Oidc/when_verifying_the_id_token_signature` | All | Published discovery key validates signature; RS256/JWT type and published key id. |
| `for_Oidc/when_requesting_userinfo` | All | Identity-only bearer accepted with only sub/profile; no storage; revoked, missing and malformed bearer refused. |
| `for_Oidc/when_matching_userinfo_to_an_openid_only_token` | All | POST bearer works; userinfo subject matches ID token; no implicit profile upgrade. |
| `for_Oidc/when_an_id_token_is_used_as_an_access_token` | All | Userinfo, introspection and resource validation reject token-type confusion; no credential aliases disclosed. |
| `for_Oidc/when_refreshing_an_identity_only_grant` | All | Subject/auth time preserved; new jti/hash, omitted nonce and rotated refresh; reuse fences successor. |
| `for_Conformance/when_discovering_identity_from_a_protected_resource` | All | Bearer challenge leads to registered resource/issuer/endpoints; valid issuance and own audience accepted, wrong audience refused. Resource host is a stub, not a product edge. |
| `for_Conformance/when_reading_a_successful_token_response` | All | Successful bearer response has positive lifetime, `no-store` and legacy `no-cache`. |
| `for_Conformance/when_redeeming_a_code_with_a_different_redirect` | All | Changed loopback port at redemption is `invalid_grant`. |
| `for_Conformance/when_redeeming_the_same_authorization_code_twice` | All | First redemption succeeds; replay is `invalid_grant`. |
| `for_Conformance/when_requesting_a_password_grant` | All | Password grant is unsupported. |
| `for_Conformance/when_requesting_an_implicit_token` | All | Implicit response type is unsupported. |
| `for_Conformance/when_revoking_an_unknown_token` | All | Successful response does not disclose token existence. |
| `for_Conformance/when_rotating_signing_keys` | All | Initial/current/eligible keys sign at the right time; overlap publishes both and preserves old access/refresh use; future key published before use; retired key removed. |
| `for_ConformanceCatalog/when_checking_the_catalog` | All | Discovered tagged facts cover all known unique requirements; expected failures are unique, reasoned and tracked, never hide MUST failures; no static conformance skips or empty tagged classes. |
| `for_ConformanceCatalog/when_validating_discovered_fact_mappings` | All | Private facts and derived theory attributes discovered; unknown/missing/empty mappings rejected; known mappings accepted. |

### Host, migration, sessions and resource integration

| Suite / scenario | Assertion scope | Specific protection |
| --- | --- | --- |
| `for_Host/when_starting_the_host` | All | Health returns `ok`; `nosniff` and frame denial remain installed. |
| `for_Host/when_starting_the_cratis_id_image` | `should_serve_liveness_and_reconciled_readiness` (all contained assertions) | **Deploy image**: real built image serves liveness and reconciled readiness. |
| `for_Host/when_starting_the_cratis_id_image_without_configuration` | `should_exit_non_zero_without_echoing_supplied_secrets` (all contained assertions) | **Deploy image; security**: incomplete configuration exits nonzero and logs do not echo supplied synthetic secret material. |
| `for_LegacyAccountStoreGuard/when_a_legacy_account_exists` | All | No startup or mutation without acknowledgement; explicit operator confirmation permits startup. |
| `for_LegacyAccountStoreGuard/when_no_legacy_account_exists` | All | Absent collection remains absent; empty collection permits startup. |
| `for_LegacyAccountStoreGuard/when_the_store_is_unreachable` | All | Startup fails within probe bounds. |
| `for_LegacyAccountStoreGuard/when_the_connection_string_is_malformed` | All | Startup fails without disclosing connection string. |
| `for_LookupRevisionGuard/when_two_instances_start_with_different_revisions` | All | Exactly one revision starts across instances. |
| `for_Introspection/when_a_client_introspects_an_access_token` | All | Only authenticated confidential resource client can introspect; audience/presenter correct; unauthenticated/invalid clients get no credential ids or epoch. |
| `for_Introspection/when_tracking_a_grant_through_introspection` | All | Stable canonical grant UUID and kind through refresh, matching advisory cid but not jti/client id; tenant/scope/epoch preserved; no invented confirmation; revoked/expired replies contain only inactive. |
| `for_Sessions/when_a_stale_session_requests_authorization` | All | Stale session with `max_age` answers `login_required` under `prompt=none` without a refreshed cookie; `max_age=0` is accepted and never reuses the stale session; malformed `max_age` is `invalid_request`. |
| `for_Sessions/when_signing_out` | All | Cross-site logout refused; valid antiforgery logout requires new login; secure/HttpOnly and distinct SameSite cookie policies. |
| `for_Sessions/when_starting_a_new_browser_session` | All | Session id/auth time survive cookie protection; existing session resumes; upstream sign-in rotates id. |
| `for_TenantsCallback/when_consent_lists_the_callback_tenants` | All | Tenant ids/names from callback and consent-selected binding. |
| `for_TenantsCallback/when_the_tenants_callback_is_down` | All | No tenant fallback or code; safe `temporarily_unavailable`. |
| `for_TenantsCallback/when_refreshing_while_the_tenants_callback_is_down` | All | Refresh fails temporarily, without tokens, and succeeds after recovery. |
| `for_TokenExchange/when_a_person_in_two_tenants_exchanges_for_each_tenant` | All | Distinct tenant-bound tokens for same external person; correct issuer/subject/resource/scope identity. |
| `for_TokenExchange/when_replay_protection_is_shared` | All | Replay reservation admits one request across hosts and restart; expired entries can be replaced without TTL cleanup, unexpired entries cannot; unreachable replay storage fails closed. |
| `for_TokenExchange/when_the_exchange_is_refused` | All | Replay, wrong client secret, nonmember tenant and unauthorized resource refused; identity-side refusals share one description. |
| `for_TokenRateLimiting/when_the_token_endpoint_is_flooded` | All | Original invalid-client result before limit; generic 429 before authentication; full-window retry; protocol validation resumes afterward. |
| `for_McpAcceptance/when_one_sign_in_authorizes_both_products` | All | One upstream sign-in permits both resources but produces separate refresh grants. |
| `for_McpAcceptance/when_product_tokens_cross_resource_boundaries` | All | Each token accepted at own resource, refused at the other. |
| `for_McpAcceptance/when_a_grant_is_revoked` | All | Retained conditional gap: both real product/edge paths must refuse revoked grants within thirty seconds. |
| `for_McpAcceptance/when_an_mcp_tool_has_no_server_owned_ceiling` | All | Retained conditional gap: tool without a server-owned scope ceiling must be refused even with a valid token. |

## Platform candidates: retain until equivalent upstream coverage is named

No upstream spec repository/path has been verified for these assertions. Their
upstream replacement is therefore **unproven**, not assumed from an API name or a
framework's general test suite. The Identity-specific assertions in the same
scenarios remain contracts even if a primitive is eventually proven upstream.

| Exact assertion selector | Additional classification | Disposition and reason |
| --- | --- | --- |
| `for_Revocation/when_replaying_source_facts_in_the_real_kernel.should_require_actual_kernel_keyed_support` | Registration vs kernel version; pinned-version | Keep: actual selected kernel capability, not a substitute/mock capability. Not a retirement candidate. |
| `for_Revocation/when_replaying_source_facts_in_the_real_kernel.should_store_exactly_one_fact_across_fresh_writers` | Platform behavior (Chronicle) | **Candidate; keep**: keyed-append deduplication overlaps a kernel primitive, but also protects Identity writer wiring. Upstream replacement unproven. |
| `for_Revocation/when_replaying_source_facts_in_the_real_kernel.should_reject_changed_content_for_the_same_fact_key` | Security; registration vs kernel version | Keep: Identity's `OutboxContentConflict` translation is not generic kernel behavior. |
| `for_AccountErasure/when_the_erasure_log_is_appended_twice_in_the_real_kernel.should_store_the_request_once` | Platform behavior (Chronicle) | **Candidate; keep**: keyed request append through Identity's erasure log; upstream replacement unproven. |
| `for_AccountErasure/when_erasing_a_tombstoned_account_in_the_real_kernel.should_no_longer_decrypt_any_of_the_erased_subjects_personal_names` | Platform behavior (Chronicle); security | **Candidate; keep**: subject-key destruction primitive, but Identity's personal-data registration and coordinator must still be exercised. Upstream replacement unproven. |
| `for_NativeAccounts/when_projection_progress_is_held.should_not_acknowledge_a_command_while_its_projection_input_is_pending` | Platform behavior (Arc/Chronicle); registration vs kernel version | **Candidate; keep**: observer-progress primitive plus Identity's registered progress fence; upstream replacement unproven. |
| `for_NativeAccounts/when_two_first_sign_ins_race.should_reject_the_stale_absence_decision_in_the_real_kernel` | Platform behavior (Arc/Chronicle); registration vs kernel version | **Candidate; keep**: uniqueness/optimistic-concurrency primitive plus Identity constraint registration; upstream replacement unproven. |
| `for_NativeAccounts/when_an_account_is_disabled_after_the_sign_in_recheck.should_reject_the_pre_disable_decision_with_real_optimistic_concurrency` | Platform behavior (Arc/Chronicle); registration vs kernel version | **Candidate; keep**: kernel concurrency plus Identity concurrency scope; upstream replacement unproven. |
| `for_NativeAccounts/when_an_account_is_tombstoned_after_the_sign_in_recheck.should_reject_the_pre_tombstone_decision_with_real_optimistic_concurrency` | Platform behavior (Arc/Chronicle); registration vs kernel version | **Candidate; keep**: same primitive, distinct tombstone/security interleaving; upstream replacement unproven. |
| `for_NativeAccounts/when_an_upstream_key_is_unlinked_after_the_sign_in_recheck.should_reject_the_pre_unlink_decision_with_real_optimistic_concurrency` | Platform behavior (Arc/Chronicle); registration vs kernel version | **Candidate; keep**: same primitive, distinct unlink/security interleaving; upstream replacement unproven. |
| `for_NativeAccounts/when_both_sign_in_attempts_lose_optimistic_concurrency.should_report_real_kernel_concurrency_violations_for_both_attempts` | Platform behavior (Arc/Chronicle); registration vs kernel version | **Candidate; keep**: both real conflicts prove the retry's kernel scope, not only the bounded retry policy; upstream replacement unproven. |

Identity's MongoDB fence log, token indexes, replay reservations, protocol policies
and exception translations are not platform-retirement candidates. No new tests
of Arc/Chronicle behavior are introduced by this map.

## CI selection and cadence

The authoritative selectors are in the linked workflows. There are no
label-only contract lanes and no added per-push heavy jobs.

| Workflow / lane | Assertions or check selected | Automatic selection and cadence |
| --- | --- | --- |
| [`build.yml`](../../.github/workflows/build.yml), `build` | Release solution build; Identity unit specs; integration executable with `-notrait "Category=Conformance"`; Node artifact/baseline specs; committed vector checks | Existing main pushes; reusable PR call selected by the `build` path set below; nightly at **05:23 UTC**. The existing Chronicle-down release-gate step also explicitly selects `for_Revocation.when_chronicle_is_unavailable`; that repetition is unchanged. |
| `build.yml`, `Protocol conformance (RFCs)` step | Integration executable with `-trait "Category=Conformance"` after non-conformance specs | Same events; a separate selection within the build job, not the full assembly repeated. Includes class/requirement traits across suites, not just `for_Conformance`. |
| [`image.yml`](../../.github/workflows/image.yml) | Build `cratis-id`; smoke selects exactly `when_starting_the_cratis_id_image` and `when_starting_the_cratis_id_image_without_configuration`, with image opt-in enabled | Existing main pushes; reusable PR call selected by the `image` path set; nightly at **06:43 UTC**. Image is loaded locally, never published by this workflow. |
| [`runner-guard.yml`](../../.github/workflows/runner-guard.yml) | Workflow runner placement and upload retention checks, **not** container/kernel assertions | Workflow changes through existing PR filter; dispatch; existing nightly **04:17 UTC**. |
| [`authorization-vectors.yml`](../../.github/workflows/authorization-vectors.yml) | Deterministic archive, vector baseline immutability, checksum and reviewed artifact packaging, **not** container/kernel assertions | Intentionally manual dispatch. The automatic build already runs its Node specs and baseline/vector checks. Packaging is not a missing integration lane. |
| [`openiddict-compat.yml`](../../.github/workflows/openiddict-compat.yml) | Disposable candidate OpenIddict build, unit specs and `Category=Conformance` | Intentionally manual dispatch. It tests the configured/latest stable candidate without changing the committed pin; the pinned contracts run automatically in `build.yml`. Do not add a nightly duplicate candidate/platform re-test here. |

[`pull-requests.yml`](../../.github/workflows/pull-requests.yml) owns PR selection.
Its path sets are conservative dependency closures of the map, rather than a
fragile list of individual assertion filenames:

| PR output | Changed paths that select it | Why these paths matter |
| --- | --- | --- |
| `build` | `Source/**`, `Identity.slnx`, `Directory.*.props`, `global.json`, `.editorconfig`, `.globalconfig`, `scripts/**`, `Documentation/specs/**`, `.github/workflows/build.yml`, `.github/workflows/pull-requests.yml` | All integration suites, fixtures, host/library/contracts and resources; unit specs; dependency/SDK/analyzer inputs; schema and vector baselines; this coverage map; build/selection wiring. |
| `image` | `Source/**`, `Docker/**`, `.dockerignore`, `Identity.slnx`, `Directory.*.props`, `global.json`, `.editorconfig`, `.globalconfig`, `Documentation/specs/container-kernel-contract-map.md`, `.github/workflows/image.yml`, `.github/workflows/pull-requests.yml` | Image smoke facts/fixtures and all hosted source; Docker context/target; dependency/SDK/analyzer inputs; this map and image/selection wiring. |
| `runner-guard` | `.github/workflows/**` | Runner and artifact-retention policy. |

Main-target PR label/edit events retain the existing full build/image rerun
behavior so they cannot cancel a code run and then satisfy the gate without
checking code. Ordinary unrelated documentation changes do not start heavy PR
jobs. The existing gate still requires the path-selector job to succeed and
rejects failed, cancelled or unknown results from every called job. Main push
coverage remains unchanged; schedules run the default branch.

The nightly schema gate compares against the checked-out committed `HEAD` schema
baseline; PR and push runs retain their existing event-specific base revisions.
Nightly runs do not manufacture a nonexistent push `before` revision.

### Explicit execution gaps

The two image facts intentionally skip in the ordinary integration lane and run
with the opt-in in the image lane. Three product/edge obligations remain
conditional and are **not claimed as covered by nightly success**:

- `for_Revocation/when_a_revoked_token_calls_a_resource_server_mcp_route`: real edge
  introspection/cache enforcement is blocked on the existing AuthProxy dependency.
- `for_McpAcceptance/when_a_grant_is_revoked`: local JWT-only resource stubs do not
  prove the real products' revocation-cache bound.
- `for_McpAcceptance/when_an_mcp_tool_has_no_server_owned_ceiling`: local stubs
  have no product dispatcher/tool policy.

Their existing conditions and issue references remain unchanged. No assertion is
removed, no skip is added and no local container execution is required by this
inventory. See [Protocol conformance](../conformance.md) for the supported profile
and the existing external-conformance limits.

## Maintaining the map

When adding or changing an assertion, update its suite/scenario mapping and the
path closure if it introduces another dependency. Before retiring a platform
candidate, record the **upstream repository, exact spec path and assertion**, show
that it protects the same behavior and selected version, and name the replacement
for every affected Identity contract. Without that evidence, keep the assertion
and the candidate marker. Passing CI alone is not upstream replacement evidence.

# End-to-end readiness checklist for Studio issue 2479

This checklist maps every step from sign-in to a denied call after revocation, for the Direct `/mcp` and Studio `/mcp` paths that Cratis ID protects. It names the component, the configuration key, the owner lane and the current state of each step, so one person can drive the whole flow.

Configuration keys are shown in appsettings form (`Identity:Tokens:AccessTokenLifetime`). The environment form replaces `:` with `__`. Secrets are referred to by key name only; never record a value here.

## Legend

| State | Meaning |
|---|---|
| done | Merged to `main`, or delivered with a named PR or commit. |
| delivered, unmerged | Built and gated on a branch; waiting for review or train merge. |
| blocked | Cannot proceed until the named input arrives. |
| done (code) | Implemented and exercised by a spec on the named ref, but no run against a deployed environment is recorded. See Evidence. |
| missing | The named gap has no implementation on `main` or a delivered branch. See Evidence. |
| unverified | No evidence in the sources; do not assume it works. |

Review of the security-sensitive items is Opus-only so far. An independent GPT-side review is queued and must finish before they merge.

## Steps

| # | Step | Component | Config key(s) | Owner lane | State | Blocker / next |
|---|---|---|---|---|---|---|
| 1 | Identity host starts with a public issuer, MongoDB and GitHub sign-in | Identity host | `Identity:Issuer`, `Identity:MongoDB:ConnectionString`, `Identity:MongoDB:Database`, `Identity:GitHub:ClientId`, `Identity:GitHub:ClientSecret` | L104 (realm, Infrastructure issue 367) with L63 (datastore egress) | done (code); deployment missing | Startup refuses a missing issuer outside Development. No evidence of a live deployment. Replicas above zero stay refused until Mongo egress exists. |
| 2 | Resource catalog lists the audiences | Identity host | `Identity:Resources:0` (direct-v1), `:1` (direct-api) in the shipped catalog | L104 | delivered, unmerged | Cratis ID image and catalog ordering spec are on train branch `train/identity-20261107-20`, not `main`. Merge waits on the GPT-side review. |
| 3 | Studio audience is optional at index 2 | Cratis ID image | `Identity:Resources:2:Indicators:0`, `Identity:Resources:2:TenantsCallback:Url`, `Identity:Resources:2:TenantsCallback:Secret` | L104 (code), L63 (values) | delivered, unmerged | Branch `identity-54-studio-resource`, head 6798518, 1649 specs passed. Unconfigured disables the audience; partial configuration fails startup. Indicator must be https and differs per environment. |
| 4 | Infra supplies the Studio indicator, callback URL and a callback secret of 32 or more characters | Infra (Core/Realm) | Pulumi: `IndicatorStudioMcp`, `TenantsCallbackUrlStudioMcp`, `TenantsCallbackSecretStudioMcp` (suggested names) | L63 | blocked | Infra knows only the two Direct entries today. Extend Core/Realm and its validation. Studio reads the same secret through `secretKeyRef`. |
| 5 | Studio tenants callback returns lowercase tenant ids and echoes `subject` | Studio | `Identity:Resources:2:TenantsCallback:Url` | L105 | done | Delivered in Studio `main` (commit cbc0bf890). Ids must match `^[a-z0-9]([a-z0-9-]{0,62}[a-z0-9])?$`. Namespaces that do not match, or collide case-insensitively, are dropped. Controller and membership specs cover malformed and case-colliding namespaces. |
| 6 | Login: user signs in with GitHub, consent, authorization code | Identity host | `Identity:SessionLifetime`, `Identity:Tokens:AuthorizationCodeLifetime` | L104 | done (code); live run missing | Defaults are 1 hour and 2 minutes. Integration specs drive sign-in, consent and code exchange. No deployed run is recorded. |
| 7 | Token: access and refresh token issued for the requested resource | Identity host | `Identity:Tokens:AccessTokenLifetime` (15 min), `Identity:Tokens:RefreshTokenLifetime` (30 days) | L104 | done (code); live run missing | Format is defined by `identity-access-token-v1`. DPoP binding is missing, see Evidence. |
| 8 | Resource server validates the token (introspection) | Identity host | `Identity:IntrospectionClients` (client id and secret per resource server) | L104 (Identity), L101 (AuthProxy, issue 255: the edge introspects for Direct and Studio) | done (Identity side); edge caller missing | Endpoint is `connect/introspect`. No resource server calls it, see Evidence. |
| 9 | Direct `/mcp` accepts the token through AuthProxy | Direct, AuthProxy | AuthProxy `Service.PassCallerAuthorizationPaths` (path list next to `AnonymousPaths`; AuthProxy #283, held, no PR); Direct pins that AuthProxy release | L101 (AuthProxy), L102 (Direct) | blocked | Since AuthProxy 2.50.1 the Authorization header is stripped on anonymous routes. The opt-in setting is decided, built and Opus-reviewed, and merges after the GPT-side review. Direct stays on 2.42.1 until then. Affected paths: `/mcp`, `/v1`, `/direct.runners.Runner`, `/webhooks/alerts`. |
| 10 | Studio `/mcp` accepts the token through its proxy | Studio mcp-auth-proxy | Cratis ID bearer route for `/mcp` (Studio issue 1342, stage B) | L105 | blocked | Not deployed. It must be an authenticated bearer route, never anonymous. Studio AuthProxy 2.51.1 is merged (PR 2583) and needs no opt-in: no Studio anonymous route reads Authorization. Live behavior after deploy is unverified. |
| 11 | Revoke: grant, session or credential is revoked and a fence is written | Identity host | Protocol in `identity-revocation-v1` (fence log, outbox, recovery epoch) | L104 | delivered, unmerged | Implementations for revocation fence and recovery are on branches `identity-41-revocation-fence` and `identity-42-revocation-recovery`; not on `main`. |
| 12 | Denied: the next call with the old token is refused on both `/mcp` paths | Direct, Studio | Resource server revocation check (unknown membership denies) | L101 (AuthProxy edge check), L103 (drives, revoke-then-call spec) | missing | Depends on steps 8 to 11. Access tokens live up to 15 minutes, so a denial must come from introspection or the fence, not from expiry. |

## Open blockers

- AuthProxy per-route caller-authorization opt-in awaits the GPT-side security review, then a release that Direct pins (step 9).
- Infra has no Studio resource configuration yet (step 4).
- Studio's `/mcp` bearer route is not deployed (step 10).
- No resource server calls introspection, and no edge applies a revocation fence (steps 8 and 12).
- DPoP issuance and edge verification are not delivered; they are after the first milestone, not blockers for it.
- Steps 2, 3 and 11 are on unmerged branches awaiting GPT-side review and the train merge.
- Steps 1, 6, 7 and 8 have no recorded live evidence.

## How to drive

Work in step order. Do not mark a step done without a PR or commit that delivers it, and for steps 6 to 8 and 12, a recorded run against a deployed environment: the URL, the time and a correlation id. For step 12, call the resource with the token, revoke, then call again and expect a refusal. Report each step as a one-line state change to the owner lane and update this table in the same change.

## Evidence

Refs are `repo@ref path:line`. Identity `main` is `origin/main`; the delivered train branch is `origin/train/identity-20261107-20`.

- **Step 1, done (code), deployment missing.** Identity@main `Source/Identity/Hosting/IdentityServiceCollectionExtensions.cs:167-169` throws `IssuerNotConfigured` outside Development; `:104` registers the GitHub upstream; `:225-227` applies the issuer. Keys are in `Source/Identity.Host/appsettings.json:11-16`. Spec `Source/Identity.Integration.Specs/for_Host/when_starting_the_host.cs:25-27` covers the health check and headers only. Gap: no deployed instance or Infra values.
- **Step 5, done.** Studio@main `Source/Core/Organizations/Mcp/CratisId/McpTenantsCallback.cs` (commit cbc0bf890): audience `studio-mcp`, bearer secret of 32 or more characters, response echoes `request.Subject`. Specs in `for_CratisIdMemberships/when_resolving_tenants/and_the_namespaces_are_not_valid_tenant_identifiers.cs`, `and_two_namespaces_differ_only_by_case.cs` and `for_McpTenantsCallbackController/when_resolving/`. Identity side: Identity@main `Source/Identity/Tenants/TenantsCallbackResponse.cs:12` holds the id pattern. Not verified: the Studio specs substitute the membership lookup, so no spec runs Identity against Studio.
- **Step 6, done (code), live run missing.** Identity@main `IdentityServiceCollectionExtensions.cs:177-183,191` (authorize endpoint, code flow, PKCE) and `:107` (session lifetime). Spec `Source/Identity.Integration.Specs/for_CliLogin/when_logging_in_with_pkce_on_an_arbitrary_loopback_port.cs:39-46` covers consent and the redirect with the code.
- **Step 7, done (code), live run missing.** Identity@main `IdentityServiceCollectionExtensions.cs:178,186,233-235` (token endpoint, unencrypted JWT, lifetimes); defaults in `Source/Identity/Configuration/TokenOptions.cs:15,20,26`. Specs: same CLI login file `:45-48` (tokens, refresh token, audience, subject); refresh rotation specs sit in `for_RefreshTokens/`.
- **Step 8, done on the Identity side; callers missing.** Identity@main `IdentityServiceCollectionExtensions.cs:180` and `Source/Identity/Clients/ClientDescriptors.cs:30`; `appsettings.json:42` ships `IntrospectionClients: []`. Spec `for_Introspection/when_a_client_introspects_an_access_token.cs:29-32`. Gap: no code in Direct, Studio, AuthProxy or Arc on `main` calls `connect/introspect`. AuthProxy bearer routes (AuthProxy PR 147, `Source/AuthProxy/BearerRoutes/BearerTokenValidator.cs`) validate the JWT locally against the issuer JWKS; Direct relies on them (Direct@main `Source/Direct.Host/Hosting/DirectApplication.cs:140`).
- **Step 12, missing.** Local JWT validation (above) has no revocation check, so a revoked token stays accepted until it expires, up to 15 minutes. The fence exists on Identity branches (step 11) but nothing at the edge consumes it (AuthProxy issue 255, open). Studio has no `/mcp` bearer route (step 10). No spec calls a resource, revokes, and calls again.
- **DPoP, missing.** Identity@main and the train branch have no DPoP code: the only matches are a secret-scan pattern and Arc MCP vectors. Identity issue 55 (issue DPoP-bound tokens) is open and branch `identity-55-dpop` is not delivered. Edge verification is AuthProxy issue 258, open. Arc and Direct have no DPoP code.

## Decisions for missing items

Decided for the first end-to-end milestone. The proposals below it are the original list; where they differ, this section wins.

- **Step 12 and step 8 (contract):** the edge checks revocation. AuthProxy issue 255 (L101): for Cratis ID bearer routes, introspect at Identity with a short positive cache (30 seconds or less, configurable), never cache negative results, and fail closed on `/mcp` when introspection is unavailable. Direct and Studio do not introspect themselves. The contract is JWKS plus edge introspection, documented under Identity issue 69.
- **Revoke-then-call spec:** owner L103, part of #2479, runs in the end-to-end.
- **Step 1 deployment:** owner L104 with L63 for the datastore egress labels. Tracked as Infrastructure issue 367, under issue 298.
- **Steps 6 and 7 live runs:** L103 runs them in the end-to-end itself; no separate issue.
- **DPoP** (Identity issue 55, AuthProxy issue 258): not part of the first milestone; after it.

## Original proposals

- Step 1 deployment: owner L63, no issue found; new issue: "Deploy the Cratis ID host with issuer, MongoDB and GitHub sign-in".
- Step 8 callers: owners L102 (Direct) and L105 (Studio) with L101 (AuthProxy); new issue: "Resource servers validate Cratis ID tokens by introspection, or document JWKS plus fence as the contract" (tied to Identity issue 69).
- Step 12 denial after revocation: owner L101 (AuthProxy), existing AuthProxy issue 255; plus Identity issue 112, "End-to-end spec: a revoked token is refused on resource-server /mcp routes".
- DPoP issuance: owner L104, Identity issue 55.
- DPoP edge verification: owner L101, AuthProxy issue 258.
- Live runs for steps 6 and 7: owner L103; new issue: "Record a deployed sign-in and token run for the 2479 checklist".

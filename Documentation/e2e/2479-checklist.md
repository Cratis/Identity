# End-to-end readiness checklist for Studio issue 2479

This checklist maps every step from sign-in to a denied call after revocation, for the Direct `/mcp` and Studio `/mcp` paths that Cratis ID protects. It names the component, the configuration key, the owner lane and the current state of each step, so one person can drive the whole flow.

Configuration keys are shown in appsettings form (`Identity:Tokens:AccessTokenLifetime`). The environment form replaces `:` with `__`. Secrets are referred to by key name only; never record a value here.

## Legend

| State | Meaning |
|---|---|
| done | Merged to `main`, or delivered with a named PR or commit. |
| delivered, unmerged | Built and gated on a branch; waiting for review or train merge. |
| blocked | Cannot proceed until the named input arrives. |
| unverified | No evidence in the sources; do not assume it works. |

Review of the security-sensitive items is Opus-only so far. An independent GPT-side review is queued and must finish before they merge.

## Steps

| # | Step | Component | Config key(s) | Owner lane | State | Blocker / next |
|---|---|---|---|---|---|---|
| 1 | Identity host starts with a public issuer, MongoDB and GitHub sign-in | Identity host | `Identity:Issuer`, `Identity:MongoDB:ConnectionString`, `Identity:MongoDB:Database`, `Identity:GitHub:ClientId`, `Identity:GitHub:ClientSecret` | L63 (Infra) | unverified | Keys exist in the host `appsettings.json` on `main`. No evidence of a live deployment. Replicas above zero stay refused until Mongo egress exists. |
| 2 | Resource catalog lists the audiences | Identity host | `Identity:Resources:0` (direct-v1), `:1` (direct-api) in the shipped catalog | L104 | delivered, unmerged | Cratis ID image and catalog ordering spec are on train branch `train/identity-20261107-20`, not `main`. Merge waits on the GPT-side review. |
| 3 | Studio audience is optional at index 2 | Cratis ID image | `Identity:Resources:2:Indicators:0`, `Identity:Resources:2:TenantsCallback:Url`, `Identity:Resources:2:TenantsCallback:Secret` | L104 (code), L63 (values) | delivered, unmerged | Branch `identity-54-studio-resource`, head 6798518, 1649 specs passed. Unconfigured disables the audience; partial configuration fails startup. Indicator must be https and differs per environment. |
| 4 | Infra supplies the Studio indicator, callback URL and a callback secret of 32 or more characters | Infra (Core/Realm) | Pulumi: `IndicatorStudioMcp`, `TenantsCallbackUrlStudioMcp`, `TenantsCallbackSecretStudioMcp` (suggested names) | L63 | blocked | Infra knows only the two Direct entries today. Extend Core/Realm and its validation. Studio reads the same secret through `secretKeyRef`. |
| 5 | Studio tenants callback returns lowercase tenant ids and echoes `subject` | Studio | `Identity:Resources:2:TenantsCallback:Url` | L105 | unverified | Ids must match `^[a-z0-9]([a-z0-9-]{0,62}[a-z0-9])?$`. Namespaces that do not match, or collide case-insensitively, are dropped. A spec was requested; no delivery is named in the sources. |
| 6 | Login: user signs in with GitHub, consent, authorization code | Identity host | `Identity:SessionLifetime`, `Identity:Tokens:AuthorizationCodeLifetime` | L104 | unverified | Defaults are 1 hour and 2 minutes. No end-to-end run is recorded. |
| 7 | Token: access and refresh token issued for the requested resource | Identity host | `Identity:Tokens:AccessTokenLifetime` (15 min), `Identity:Tokens:RefreshTokenLifetime` (30 days) | L104 | unverified | Format is defined by `identity-access-token-v1`. DPoP binding is a separate spec (`identity-dpop-v1`); its delivery state is not in the sources. |
| 8 | Resource server validates the token (introspection) | Identity host | `Identity:IntrospectionClients` (client id and secret per resource server) | L104 (Identity), L102 (Direct), L105 (Studio) | unverified | Endpoint is `connect/introspect`. No resource server entry is recorded as configured. |
| 9 | Direct `/mcp` accepts the token through AuthProxy | Direct, AuthProxy | AuthProxy route setting `PassCallerAuthorization` on anonymous routes; Direct pins that AuthProxy release | L101 (AuthProxy), L102 (Direct) | blocked | Since AuthProxy 2.50.1 the Authorization header is stripped on anonymous routes. The opt-in setting is decided, built and Opus-reviewed, and merges after the GPT-side review. Direct stays on 2.42.1 until then. Affected paths: `/mcp`, `/v1`, `/direct.runners.Runner`, `/webhooks/alerts`. |
| 10 | Studio `/mcp` accepts the token through its proxy | Studio mcp-auth-proxy | Cratis ID bearer route for `/mcp` (Studio issue 1342, stage B) | L105 | blocked | Not deployed. It must be an authenticated bearer route, never anonymous. Studio AuthProxy 2.51.1 is merged (PR 2583) and needs no opt-in: no Studio anonymous route reads Authorization. Live behavior after deploy is unverified. |
| 11 | Revoke: grant, session or credential is revoked and a fence is written | Identity host | Protocol in `identity-revocation-v1` (fence log, outbox, recovery epoch) | L104 | delivered, unmerged | Implementations for revocation fence and recovery are on branches `identity-41-revocation-fence` and `identity-42-revocation-recovery`; not on `main`. |
| 12 | Denied: the next call with the old token is refused on both `/mcp` paths | Direct, Studio | Resource server revocation check (unknown membership denies) | L102, L105, L103 (drives) | unverified | Depends on steps 8 to 11. Access tokens live up to 15 minutes, so a denial must come from introspection or the fence, not from expiry. |

## Open blockers

- AuthProxy per-route caller-authorization opt-in awaits the GPT-side security review, then a release that Direct pins (step 9).
- Infra has no Studio resource configuration yet (step 4).
- Studio's `/mcp` bearer route is not deployed (step 10).
- Studio tenants callback conformance has no named delivery (step 5).
- Steps 2, 3 and 11 are on unmerged branches awaiting GPT-side review and the train merge.
- Steps 1, 6, 7, 8 and 12 have no recorded live evidence.

## How to drive

Work in step order. Do not mark a step done without a PR or commit that delivers it, and for steps 6 to 8 and 12, a recorded run against a deployed environment: the URL, the time and a correlation id. For step 12, call the resource with the token, revoke, then call again and expect a refusal. Report each step as a one-line state change to the owner lane and update this table in the same change.

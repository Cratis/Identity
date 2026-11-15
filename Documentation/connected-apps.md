---
title: Manage connected apps
description: List and revoke an account's connected apps and add the optional React UI.
---

Cratis Identity exposes account-owned connected-app listing and revocation through Arc:

- `ConnectedApp.ForCurrentAccount` lists the session account's valid OpenIddict authorizations that have no committed grant fence. It returns authorization id, public client id, display name (client-id fallback), scopes and creation time. Results are capped at 1000, newest first, then ordered by authorization id. Anonymous callers are refused; an authenticated but malformed account subject returns an empty list.
- `RevokeConnectedApp` accepts only an authorization id. It resolves the owner through the realm-bound operational authorization manager, not the read model. Unknown and foreign ids receive the same refusal. Anonymous callers, malformed subjects and empty ids cannot reach fence mutation.

Both use the host's existing authenticated Arc query/command transport and realm middleware, like linked-account management. They do not expose anonymous protocol endpoints. Hosts must retain the same-origin/session and CSRF protections used for their other authenticated Arc commands. No account or realm selector is accepted from a request body.

The query is a display surface, never credential authority. Operational authorization/application managers supply the metadata and the fence store suppresses fenced grants, including a fence committed before operational status catches up. Fence-read failures propagate rather than claiming an authoritative active list. Dormant, abandoned and revoked grants are not active connected apps.

Revocation reuses `GrantRevocation`: commit the whole-grant fence, revoke all refresh families/tokens and confirm the independent durable log before success. A retry confirms the same durable fence. Operational or log failures do not report success. Self-contained access tokens may remain valid for up to 10 minutes plus 30 seconds of allowed clock skew; resources with stricter revocation enforcement may reject them earlier.

## Clients

The generated, framework-neutral proxies are exported from `@cratis/identity/Authorization/ConnectedApps`. They do not introduce a React dependency.

For an optional unstyled React UI, install `@cratis/identity.react` and configure the host's Arc transport and signed-in session before rendering `ConnectedApps`. The package provides confirmation, pending/duplicate-submit protection, errors and retry, and refresh after a server-confirmed revoke. It never reads tokens or stores credentials. See the [React package guide](../Source/JavaScript/Identity.React/README.md) for installation, styling and headless view-model use.

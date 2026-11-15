# @cratis/identity.react

Optional, unstyled React views for a signed-in Cratis Identity account. `@cratis/identity` remains a framework-neutral proxy package; installing it does not install this UI.

## Install

CI currently builds and packs both Identity packages as artifacts only; registry publication is not enabled. Install the CI tarballs while the packages remain private. Once released, the equivalent registry installation is:

```sh
npm install @cratis/identity.react @cratis/identity @cratis/arc @cratis/arc.react react
```

Use the same Arc and Identity versions as the host. React 18 and 19 are supported. Configure the host's Arc transport, session authentication and realm base URL before mounting the component. Use the same same-origin request/CSRF protection as the host's other authenticated Arc commands; this package does not create cookies, attach tokens, select an account or change realms.

```tsx
import { ConnectedApps } from '@cratis/identity.react';

export function AccountSettings() {
    return <ConnectedApps className="account-connected-apps" />;
}
```

The view loads client names, identifiers, approved scopes and grant times for the current session account. The server returns the newest 1000 active, unfenced grants, ordered by grant time then authorization id. The list is display-only: neither an empty list nor its cached contents authorize any operation. A fence-store failure is an error, not an empty successful list.

Revoke requires confirmation and sends only the authorization id. The server rechecks ownership against the realm-bound operational authorization manager and confirms its durable grant fence before acknowledging success. Failed revocations remain retryable; the UI refreshes only after a successful command. Duplicate submissions are disabled while pending. Revocation blocks refresh for the entire grant and all its families. Already-issued self-contained access tokens may remain valid for up to 10 minutes plus 30 seconds of allowed clock skew unless the resource uses stricter enforcement.

## Customize

`ConnectedApps` accepts `heading`, `className` and `renderApp` for display customization. Names and scopes are rendered as ordinary React text by default; custom renderers must maintain the host's escaping and accessibility policy. No CSS or design-system dependency is bundled.

For a host-owned view, use `useConnectedApps()` and bind the returned `apps`, loading/error/pending state, `select`, `cancel`, `confirm` and `refresh` actions. `ConnectedAppsView` also accepts that model as a presentation-only component. Confirmation, status messages, retry controls and buttons have accessible native semantics; no modal focus management is needed because confirmation is inline.

`@cratis/identity/Authorization/ConnectedApps` exports the generated Arc query/command proxies for non-React hosts. This optional package contains no token storage, bearer-token access, credential prompts or protocol endpoints. Dormant grants are not listed; this package does not implement dormant-grant reactivation.

## Develop

With the framework-neutral sibling package built:

```sh
corepack yarn install --immutable
corepack yarn compile
corepack yarn lint
corepack yarn test
```

The package pins Yarn through `packageManager` and uses a committed lockfile. Build output externalizes React and all Cratis peers.

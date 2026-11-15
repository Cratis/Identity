// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ReactNode } from 'react';
import type { ConnectedApp } from '@cratis/identity/Authorization/ConnectedApps';
import { type ConnectedAppsViewModel, useConnectedApps } from './ConnectedAppsViewModel';

/** Host styling and display customization; identifiers and scope labels remain escaped React text. */
export interface ConnectedAppsOptions {
    className?: string;
    heading?: string;
    renderApp?: (app: ConnectedApp) => ReactNode;
}

/** Optional ready-to-use view backed by the host-configured Arc transport. */
export function ConnectedApps(options: ConnectedAppsOptions) {
    const model = useConnectedApps();
    return <ConnectedAppsView {...options} model={model} />;
}

/** Presentation-only view; hosts can use the view model to render their own design system instead. */
export function ConnectedAppsView({ model, className, heading = 'Connected apps', renderApp }: ConnectedAppsOptions & { model: ConnectedAppsViewModel }) {
    return <section className={className} aria-label={heading} aria-busy={model.loading || model.pending}>
        <h2>{heading}</h2>
        {model.loading && <p role="status">Loading connected apps…</p>}
        {model.loadFailed && <p role="alert">Could not load connected apps. <button type="button" onClick={model.refresh}>Try again</button></p>}
        {!model.loading && !model.loadFailed && model.apps.length === 0 && <p>No connected apps.</p>}
        {!model.loadFailed && <ul>{model.apps.map(app => <li key={app.authorizationId}>
            {renderApp ? renderApp(app) : <>
                <h3>{app.displayName}</h3>
                <p>{app.clientId}</p>
                <p>Scopes: {app.scopes.join(', ')}</p>
                <p>Granted: <time dateTime={app.grantedAt.toISOString()}>{app.grantedAt.toLocaleString()}</time></p>
            </>}
            <button type="button" aria-label={`Revoke ${app.displayName}`} disabled={model.pending} onClick={() => model.select(app)}>Revoke</button>
        </li>)}</ul>}
        {model.selected && <div role="group" aria-label="Confirm app revocation">
            <p>Revoke access for {model.selected.displayName}?</p>
            <p>Self-contained access tokens may remain valid for up to 10 minutes plus allowed clock skew. Stricter resource enforcement may reject them sooner.</p>
            <button type="button" disabled={model.pending} onClick={() => { void model.confirm(); }}>{model.pending ? 'Revoking…' : 'Confirm revoke'}</button>
            <button type="button" disabled={model.pending} onClick={model.cancel}>Cancel</button>
        </div>}
        {model.revokeFailed && <p role="alert">Could not confirm revocation. Try again.</p>}
    </section>;
}

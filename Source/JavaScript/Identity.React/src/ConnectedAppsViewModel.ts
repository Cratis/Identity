// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useRef, useState } from 'react';
import { ConnectedApp, ForCurrentAccount, RevokeConnectedApp } from '@cratis/identity/Authorization/ConnectedApps';

/** Display and interaction state, with no credential or authorization decisions. */
export interface ConnectedAppsViewModel {
    apps: readonly ConnectedApp[];
    loading: boolean;
    loadFailed: boolean;
    revokeFailed: boolean;
    pending: boolean;
    selected?: ConnectedApp;
    refresh(): void;
    select(app: ConnectedApp): void;
    cancel(): void;
    confirm(): Promise<void>;
}

/** Uses the host's Arc session/transport; success means the server confirmed its durable fence. */
export function useConnectedApps(): ConnectedAppsViewModel {
    const [query, refresh] = ForCurrentAccount.use();
    const [command] = RevokeConnectedApp.use();
    const [selected, select] = useState<ConnectedApp>();
    const [pending, setPending] = useState(false);
    const [revokeFailed, setRevokeFailed] = useState(false);
    const inFlight = useRef(false);

    async function confirm() {
        if (!selected || inFlight.current) return;
        inFlight.current = true;
        setPending(true);
        setRevokeFailed(false);
        try {
            command.authorizationId = selected.authorizationId;
            const result = await command.execute();
            if (!result.isSuccess) {
                setRevokeFailed(true);
                return;
            }
            select(undefined);
            await refresh();
        } catch {
            setRevokeFailed(true);
        } finally {
            inFlight.current = false;
            setPending(false);
        }
    }

    return {
        apps: query.data,
        loading: query.isPerforming,
        loadFailed: !query.isPerforming && !query.isSuccess,
        revokeFailed,
        pending,
        selected,
        refresh: () => { void refresh(); },
        select: app => { if (!inFlight.current) { setRevokeFailed(false); select(app); } },
        cancel: () => { if (!inFlight.current) select(undefined); },
        confirm
    };
}

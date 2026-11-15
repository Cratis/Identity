// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ConnectedAppsView } from '../ConnectedApps';
import type { ConnectedAppsViewModel } from '../ConnectedAppsViewModel';

vi.mock('@cratis/identity/Authorization/ConnectedApps', () => ({}));
afterEach(cleanup);

const app = { authorizationId: 'grant', clientId: 'calendar', displayName: '<script>Calendar</script>', scopes: ['openid', 'profile'], grantedAt: new Date('2026-11-01T12:00:00Z') };

function model(overrides: Partial<ConnectedAppsViewModel> = {}): ConnectedAppsViewModel {
    return { apps: [], loading: false, loadFailed: false, revokeFailed: false, pending: false, refresh: vi.fn(), select: vi.fn(), cancel: vi.fn(), confirm: vi.fn(), ...overrides };
}

describe('when rendering connected apps', () => {
    it('shows loading without claiming the list is empty', () => {
        render(<ConnectedAppsView model={model({ loading: true })} />);
        expect(screen.getByRole('status').textContent).toContain('Loading');
        expect(screen.queryByText('No connected apps.')).toBeNull();
    });

    it('shows the empty state', () => {
        render(<ConnectedAppsView model={model()} />);
        expect(screen.getByText('No connected apps.')).toBeDefined();
    });

    it('shows a retry for load failures', () => {
        const state = model({ loadFailed: true });
        render(<ConnectedAppsView model={state} />);
        fireEvent.click(screen.getByRole('button', { name: 'Try again' }));
        expect(state.refresh).toHaveBeenCalledOnce();
    });

    it('renders metadata as text and requests confirmation before revoking', () => {
        const state = model({ apps: [app] });
        const { container } = render(<ConnectedAppsView model={state} />);
        expect(screen.getByText(app.displayName)).toBeDefined();
        expect(container.querySelector('script')).toBeNull();
        expect(screen.getByText('Scopes: openid, profile')).toBeDefined();
        expect(container.querySelector('time')?.dateTime).toBe(app.grantedAt.toISOString());
        fireEvent.click(screen.getByRole('button', { name: `Revoke ${app.displayName}` }));
        expect(state.select).toHaveBeenCalledWith(app);
        expect(state.confirm).not.toHaveBeenCalled();
    });

    it('explains the residual access-token lifetime and permits cancellation', () => {
        const state = model({ apps: [app], selected: app });
        render(<ConnectedAppsView model={state} />);
        expect(screen.getByText(/up to 10 minutes/)).toBeDefined();
        fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
        expect(state.cancel).toHaveBeenCalledOnce();
        expect(state.confirm).not.toHaveBeenCalled();
    });

    it('disables repeat submission while pending', () => {
        const state = model({ apps: [app], selected: app, pending: true });
        render(<ConnectedAppsView model={state} />);
        const button = screen.getByRole('button', { name: 'Revoking…' }) as HTMLButtonElement;
        expect(button.disabled).toBe(true);
        fireEvent.click(button);
        expect(state.confirm).not.toHaveBeenCalled();
    });

    it('does not claim success on a failed revoke', () => {
        render(<ConnectedAppsView model={model({ selected: app, revokeFailed: true })} />);
        expect(screen.getByRole('alert').textContent).toContain('Could not confirm revocation');
        expect(screen.getByRole('button', { name: 'Confirm revoke' })).toBeDefined();
    });
});

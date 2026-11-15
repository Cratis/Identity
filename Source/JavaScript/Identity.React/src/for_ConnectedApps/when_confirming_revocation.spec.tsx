// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, cleanup, renderHook } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { useConnectedApps } from '../ConnectedAppsViewModel';

const transport = vi.hoisted(() => ({
    refresh: vi.fn(),
    command: { authorizationId: '', execute: vi.fn() },
    query: { data: [], isPerforming: false, isSuccess: true }
}));
vi.mock('@cratis/identity/Authorization/ConnectedApps', () => ({
    ForCurrentAccount: { use: () => [transport.query, transport.refresh] },
    RevokeConnectedApp: { use: () => [transport.command] }
}));

const app = { authorizationId: 'grant', clientId: 'calendar', displayName: 'Calendar', scopes: ['openid'], grantedAt: new Date() };
afterEach(cleanup);
beforeEach(() => {
    vi.clearAllMocks();
    transport.command.execute.mockResolvedValue({ isSuccess: true });
});

describe('when confirming connected-app revocation', () => {
    it('submits only the authorization id and refreshes after server success', async () => {
        const { result } = renderHook(useConnectedApps);
        act(() => result.current.select(app));
        await act(() => result.current.confirm());
        expect(transport.command.authorizationId).toBe('grant');
        expect(transport.command.execute).toHaveBeenCalledOnce();
        expect(transport.refresh).toHaveBeenCalledOnce();
        expect(result.current.selected).toBeUndefined();
    });

    it('retains confirmation for retry and does not refresh after server failure', async () => {
        transport.command.execute.mockResolvedValue({ isSuccess: false });
        const { result } = renderHook(useConnectedApps);
        act(() => result.current.select(app));
        await act(() => result.current.confirm());
        expect(transport.refresh).not.toHaveBeenCalled();
        expect(result.current.revokeFailed).toBe(true);
        expect(result.current.selected).toBe(app);
    });

    it('retains confirmation for retry on a network failure', async () => {
        transport.command.execute.mockRejectedValue(new Error('network'));
        const { result } = renderHook(useConnectedApps);
        act(() => result.current.select(app));
        await act(() => result.current.confirm());
        expect(result.current.revokeFailed).toBe(true);
        expect(result.current.pending).toBe(false);
        expect(transport.refresh).not.toHaveBeenCalled();
    });

    it('guards synchronous double submissions until the server responds', async () => {
        let finish!: (result: { isSuccess: boolean }) => void;
        transport.command.execute.mockReturnValue(new Promise(resolve => { finish = resolve; }));
        const { result } = renderHook(useConnectedApps);
        act(() => result.current.select(app));
        let first!: Promise<void>;
        act(() => { first = result.current.confirm(); });
        await act(() => result.current.confirm());
        expect(transport.command.execute).toHaveBeenCalledOnce();
        expect(result.current.pending).toBe(true);
        await act(async () => { finish({ isSuccess: true }); await first; });
        expect(result.current.pending).toBe(false);
    });

    it('does not submit before explicit confirmation', async () => {
        const { result } = renderHook(useConnectedApps);
        act(() => result.current.select(app));
        act(() => result.current.cancel());
        await act(() => result.current.confirm());
        expect(transport.command.execute).not.toHaveBeenCalled();
    });
});

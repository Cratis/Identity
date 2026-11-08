// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding.given;

internal class MemoryAuthorizations : IAuthorizationActivator
{
    readonly Lock _gate = new();
    readonly Dictionary<ConsentRequestId, string> _prepared = [];
    public Action? BeforePrepare;
    public int Count => _prepared.Count;

    public Task<string?> Prepare(ConsentRequest request, CancellationToken cancellationToken)
    {
        BeforePrepare?.Invoke();
        lock (_gate)
        {
            if (!_prepared.TryGetValue(request.Id, out var id)) _prepared[request.Id] = id = request.Id.Value.ToString();
            return Task.FromResult<string?>(id);
        }
    }

    public async Task<bool> Usable(ConsentRequestId id, IConsentRequestStore store, IRevocationFence fence)
    {
        var row = await store.Read(id, CancellationToken.None);
        return row is { State: ConsentRequestState.Active, AuthorizationId: not null } && await fence.IsClear(row, CancellationToken.None);
    }
}

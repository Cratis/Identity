// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.ConsentFlow.Deciding.given;

internal class MemoryFence : IRevocationFence
{
    public bool Clear = true;
    public bool Fail;

    public Task<bool> IsClear(ConsentRequest request, CancellationToken cancellationToken) =>
        Fail ? throw new InvalidOperationException("Fence unavailable") : Task.FromResult(Clear);
}

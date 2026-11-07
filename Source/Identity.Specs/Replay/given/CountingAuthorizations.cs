// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.ConsentFlow;

namespace Cratis.Identity.Replay.given;

internal sealed class CountingAuthorizations(IAuthorizationActivator inner) : IAuthorizationActivator
{
    internal int Prepares { get; private set; }
    internal bool Withhold { get; set; }

    public Task<string?> Prepare(ConsentRequest request, CancellationToken cancellationToken)
    {
        Prepares++;
        return Withhold ? Task.FromResult<string?>(null) : inner.Prepare(request, cancellationToken);
    }
}

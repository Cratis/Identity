// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Replay.given;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.ConsentFlow.Replay.given;

public class a_replayed_activation : Deciding.given.an_activation
{
    internal CountingAuthorizations counting;
    internal ConsentRequest after;
    internal bool usable;
    internal string? code;

    void Establish()
    {
        counting = new(authorizations);
        RestartReplay();
    }

    internal void RestartReplay() => activation = new(store, evidence, fence, counting, clock, Options.Create(options), gate);

    internal async Task ReplayTwice()
    {
        for (var pass = 0; pass < 2; pass++)
        {
            await evidence.Record(request, CancellationToken.None);
            RestartReplay();
            after = await Resume();
        }

        usable = await authorizations.Usable(request.Id, store, fence);
        code = await Claim();
    }
}

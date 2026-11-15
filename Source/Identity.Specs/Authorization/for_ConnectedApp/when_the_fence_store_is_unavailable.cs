// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.ConnectedApps;
using Cratis.Identity.Revocation;

namespace Cratis.Identity.Authorization.for_ConnectedApp;

public class when_the_fence_store_is_unavailable : given.connected_grants
{
    Exception _error;

    void Establish()
    {
        Grants(Grant("grant", DateTimeOffset.UnixEpoch));
        _fences.Read("grant", Arg.Any<CancellationToken>()).Returns(Task.FromException<GrantState?>(new InvalidOperationException("unavailable")));
    }

    async Task Because() => _error = await Catch.Exception(() => ConnectedApp.ForCurrentAccount(_context, _reader));

    [Fact] void should_not_claim_an_active_list() => _error.ShouldBeOfExactType<InvalidOperationException>();
}

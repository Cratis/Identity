// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Identity.Clients.for_EventSourcedClientRegistrations;

public class when_sweeping_declared_active_clients : given.a_sweeping_client_store
{
    ObjectId[] _ids;

    async Task Establish()
    {
        await Reconcile(_cli, _confidential);
        _ids = _applications.Keys.Order().ToArray();
    }

    async Task Because() => await Reconcile(_cli, _confidential);

    [Fact] void should_preserve_the_active_declared_materializations() => _applications.Keys.Order().ShouldEqual(_ids);
    [Fact] void should_keep_both_the_public_and_confidential_clients() => _applications.Count.ShouldEqual(2);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.Lookup;

/// <summary>Indexes Internal facts under the account owner; unlink never releases a reservation.</summary>
internal class UpstreamKeyLookupProjection : IProjectionFor<UpstreamKeyLookup>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<UpstreamKeyLookup> builder) => builder
        .From<UpstreamKeyLinkRecorded>(from => from.Set(model => model.Id).ToEventSourceId())
        .Children(model => model.Keys, children => children
            .IdentifiedBy(key => key.Id)
            .From<UpstreamKeyLinkRecorded>(from => from
                .UsingKey(@event => @event.LinkId)
                .Set(key => key.Id).To(@event => @event.LinkId)
                .Set(key => key.Active).ToValue(true))
            .From<UpstreamKeyUnlinkRecorded>(from => from
                .UsingKey(@event => @event.LinkId)
                .Set(key => key.Active).ToValue(false)));
}

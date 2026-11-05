// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.Lookup;

/// <summary>Indexes Internal link facts without deleting reservations on unlink.</summary>
public class UpstreamKeyLookupProjection : IProjectionFor<UpstreamKeyLookup>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<UpstreamKeyLookup> builder) => builder
        .From<UpstreamKeyLinkRecorded>(from => from
            .UsingKey(@event => @event.KeyHash)
            .Set(model => model.Id).To(@event => @event.KeyHash)
            .Set(model => model.AccountSubject).ToEventSourceId()
            .Set(model => model.Active).ToValue(true))
        .From<UpstreamKeyUnlinkRecorded>(from => from
            .UsingKey(@event => @event.KeyHash)
            .Set(model => model.Active).ToValue(false));
}

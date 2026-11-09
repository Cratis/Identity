// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.Constraints;

/// <summary>Reserves the exact upstream pair across all accounts, even after unlink.</summary>
internal class UniqueUpstreamKey : IConstraint
{
    /// <inheritdoc/>
    public void Define(IConstraintBuilder builder) => builder.Unique(unique => unique
        .WithName("unique-upstream-key")
        .WithMessage("The upstream key is already reserved.")
        .WithMode(UniqueConstraintMode.PerValue)
        .On<UpstreamKeyLinkRecorded>(@event => @event.KeyHash));
}

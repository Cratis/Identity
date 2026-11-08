// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>The erasure decision state of one account, folded from its stream.</summary>
internal record ErasureState
{
    /// <summary>Gets whether the subject is permanently tombstoned, which carries its revocation fence.</summary>
    public bool Tombstoned { get; init; }

    /// <summary>Gets the accepted request, if any.</summary>
    public ErasureId? Requested { get; init; }

    /// <summary>Gets whether the approved boundary was completed.</summary>
    public bool Completed { get; init; }

    /// <summary>Gets the stage and reason of the last recorded failure of the current request.</summary>
    public (ErasureStageId Stage, ReasonId Reason)? LastFailure { get; init; }

    /// <summary>Folds an account's events in stream order.</summary>
    /// <param name="events">The stream.</param>
    /// <returns>The state.</returns>
    internal static ErasureState Fold(IEnumerable<object> events) => events.Aggregate(new ErasureState(), Apply);

    static ErasureState Apply(ErasureState state, object @event) => @event switch
    {
        AccountTombstoneRecorded => state with { Tombstoned = true },
        AccountErasureRequested requested => state with { Requested = requested.ErasureId, LastFailure = null },
        AccountErasureFailed failed => state with { LastFailure = (failed.StageId, failed.ReasonId) },
        AccountErasureRecorded => state with { Completed = true, LastFailure = null },
        _ => state
    };
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Identity.Revocation;

/// <summary>Starts closed. Neither retry exhaustion nor an outage opens it.</summary>
internal sealed class RecoveryGate
{
    int _ready;
    int _grantsRecovered;
    int _restored;
    long _epoch;
    long _closedAt = Stopwatch.GetTimestamp();

    /// <summary>Gets whether independent recovery coverage is proven.</summary>
    internal bool Ready => Volatile.Read(ref _ready) == 1;

    /// <summary>Gets whether this host has observed a recovery epoch.</summary>
    internal bool Restored => Volatile.Read(ref _restored) == 1;

    /// <summary>Gets the last independently observed epoch.</summary>
    internal long Epoch => Interlocked.Read(ref _epoch);

    /// <summary>Gets whether intact, never-restored legacy adoption is permitted.</summary>
    internal bool AllowLegacyBootstrap => Ready && !Restored && Epoch == 0;

    /// <summary>Gets the monotonic duration of the current unready interval.</summary>
    internal double UnreadySeconds => Ready ? 0 : Stopwatch.GetElapsedTime(Interlocked.Read(ref _closedAt)).TotalSeconds;

    /// <summary>Closes admission without erasing continuity evidence.</summary>
    internal void Close()
    {
        Interlocked.Exchange(ref _grantsRecovered, 0);
        if (Interlocked.Exchange(ref _ready, 0) == 1)
        {
            Interlocked.Exchange(ref _closedAt, Stopwatch.GetTimestamp());
        }
    }

    /// <summary>Observes independent custody without permitting epoch rollback.</summary>
    /// <param name="epoch">The current independent epoch.</param>
    /// <exception cref="RevocationPending">The observed epoch moved backward.</exception>
    internal void ObserveEpoch(long epoch)
    {
        if (epoch < Epoch)
        {
            Close();
            throw new RevocationPending();
        }

        if (epoch != Epoch)
        {
            Close();
        }

        Interlocked.Exchange(ref _epoch, epoch);
        if (epoch > 0)
        {
            Volatile.Write(ref _restored, 1);
        }
    }

    /// <summary>Requires complete continuity coverage before creating a capability.</summary>
    /// <param name="restriction">The required restriction class.</param>
    /// <exception cref="RevocationPending">Recovery is not ready.</exception>
    internal void Demand(RestrictionClass restriction)
    {
        if (!Allows(restriction)) throw new RevocationPending();
    }

    /// <summary>Marks completion of independent grant replay in the observed epoch.</summary>
    internal void MarkGrantsRecovered() => Interlocked.Exchange(ref _grantsRecovered, 1);

    /// <summary>Reports coverage without opening admission for uncovered classes.</summary>
    /// <param name="restriction">The required restriction class.</param>
    /// <returns>Whether the class has complete recovery coverage.</returns>
    internal bool Allows(RestrictionClass restriction) => Ready || (Volatile.Read(ref _grantsRecovered) != 0 && Covered(restriction));

    /// <summary>Checks an operational capability against the independently observed epoch.</summary>
    /// <param name="epoch">The capability's immutable creation epoch.</param>
    /// <returns>Whether the capability is usable.</returns>
    internal bool Accepts(long epoch) => Allows(RestrictionClass.Consent) && epoch >= 0 && epoch == Epoch;

    /// <summary>Opens admission only with complete restriction coverage.</summary>
    /// <param name="restrictionsProven">Whether all restriction classes were proven.</param>
    internal void Open(bool restrictionsProven)
    {
        // The covered grant log is not proof of other restriction classes after a restore.
        if (!restrictionsProven)
        {
            Close();
            return;
        }

        Volatile.Write(ref _ready, 1);
    }

    static bool Covered(RestrictionClass restriction) => restriction is
        RestrictionClass.Grants or RestrictionClass.Tokens or RestrictionClass.Authorizations or
        RestrictionClass.Consent or RestrictionClass.Keys or RestrictionClass.ClientSecrets or RestrictionClass.LegacyBootstrap;
}

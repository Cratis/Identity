// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Identity.Revocation;
using Cratis.Identity.RevocationSpecs;

namespace Cratis.Identity.Replay.given;

internal sealed class RevocationReplay
{
    internal readonly SharedFenceStore Fences = new();
    internal GrantState Before = null!;
    internal GrantState After = null!;
    internal RedeemOutcome Redemption;
    internal int Appended;

    internal async Task Run()
    {
        var initial = await Fences.RecordFamilyStart("grant", "presented-entry", FenceDoubles.Subject, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1), CancellationToken.None);
        await Fences.RedeemAndInstall("grant", "presented-entry", "successor-entry", initial.Version, CancellationToken.None);
        await Fences.Commit("grant", FenceDoubles.Subject, RevocationReason.RefreshTokenReused, CancellationToken.None);
        Before = await Fences.Read("grant", CancellationToken.None);
        using var publications = new PublicationReplay();
        var writer = new ChronicleRevocationFactWriter(publications.Store);
        foreach (var entry in Before.Outbox) await writer.Append(entry, CancellationToken.None);
        await new ReadModelRebuild().Rebuild();
        foreach (var entry in Before.Outbox) await new ChronicleRevocationFactWriter(publications.Store).Append(entry, CancellationToken.None);
        Appended = (await PublicationReplay.Read(publications.Store.GetEventSequence(EventSequenceId.Log))).Length;
        Redemption = await Fences.RedeemAndInstall("grant", "successor-entry", "forbidden-entry", Before.Version, CancellationToken.None);
        After = await Fences.Read("grant", CancellationToken.None);
    }
}

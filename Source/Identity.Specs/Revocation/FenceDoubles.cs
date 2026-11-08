// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Revocation;

namespace Cratis.Identity.RevocationSpecs;

internal static class FenceDoubles
{
    internal const string Subject = "2e08ca46-2146-4ae2-a8d8-f288f6805158";

    internal static IFenceStore Store(string grantId, string tokenId, DateTimeOffset now)
    {
        var store = Substitute.For<IFenceStore>();
        var entry = OutboxEntry.Create(FactKind.FamilyStarted, grantId, Subject, now, now.AddDays(30));
        var state = new GrantState(grantId, 1, tokenId, null, [entry]);
        store.Read(grantId, Arg.Any<CancellationToken>()).Returns(state);
        store.RedeemAndInstall(grantId, tokenId, Arg.Any<string>(), 1, Arg.Any<CancellationToken>()).Returns(RedeemOutcome.Installed);
        store.Commit(grantId, Subject, Arg.Any<RevocationReason>(), Arg.Any<CancellationToken>())
            .Returns(call => new GrantFence(Guid.NewGuid(), 1, grantId, Subject, call.Arg<RevocationReason>(), now));
        return store;
    }

    internal static ConfirmFenceLog Confirmation(IFenceStore store)
    {
        var log = Substitute.For<IFenceLog>();
        log.Append(Arg.Any<GrantFence>(), Arg.Any<CancellationToken>()).Returns(call => call.Arg<GrantFence>().Sequence);
        return new(store, log);
    }
}

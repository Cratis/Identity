// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts.Linking;

/// <summary>Admits a guarded source read without relying on reducer admission in Chronicle 19.111.</summary>
public class AccountGuardProjection : IProjectionFor<AccountGuard>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<AccountGuard> builder) => builder
        .From<AccountRegistrationRecorded>(from => from.Set(model => model.Id).ToEventSourceId())
        .From<AccountDisablementRecorded>(from => from.Set(model => model.Id).ToEventSourceId())
        .From<AccountEnablementRecorded>(from => from.Set(model => model.Id).ToEventSourceId())
        .From<AccountTombstoneRecorded>(from => from.Set(model => model.Id).ToEventSourceId())
        .From<AccountEmailChanged>(from => from.Set(model => model.Id).ToEventSourceId())
        .From<AccountNameChanged>(from => from.Set(model => model.Id).ToEventSourceId())
        .From<UpstreamKeyLinkProposed>(from => from.Set(model => model.Id).ToEventSourceId())
        .From<UpstreamKeyLinkQuarantined>(from => from.Set(model => model.Id).ToEventSourceId())
        .From<UpstreamKeyLinkCleared>(from => from.Set(model => model.Id).ToEventSourceId())
        .From<UpstreamKeyLinkRejected>(from => from.Set(model => model.Id).ToEventSourceId())
        .From<UpstreamKeyLinkRecorded>(from => from.Set(model => model.Id).ToEventSourceId())
        .From<UpstreamKeyUnlinkRecorded>(from => from.Set(model => model.Id).ToEventSourceId())
        .From<UpstreamKeySubjectRecorded>(from => from.Set(model => model.Id).ToEventSourceId())
        .From<UpstreamKeyDisplayIdentifierObserved>(from => from.Set(model => model.Id).ToEventSourceId());
}

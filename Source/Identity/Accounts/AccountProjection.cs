// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts;

/// <summary>Projects private account facts and raw linked keys under the canonical account owner.</summary>
public class AccountProjection : IProjectionFor<Account>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<Account> builder) => builder
        .From<AccountRegistrationRecorded>(from => from
            .Set(model => model.Id).ToEventSourceId()
            .Set(model => model.Status).ToValue(AccountStatus.Active))
        .From<AccountNameChanged>(from => from.Set(model => model.Name).To(@event => @event.PersonalName))
        .From<AccountEmailChanged>()
        .From<AccountDisablementRecorded>(from => from.Set(model => model.Status).ToValue(AccountStatus.Disabled))
        .From<AccountEnablementRecorded>(from => from.Set(model => model.Status).ToValue(AccountStatus.Active))
        .From<AccountTombstoneRecorded>(from => from.Set(model => model.Status).ToValue(AccountStatus.Tombstoned))
        .From<UpstreamKeySubjectRecorded>(from => from.Set(model => model.GitHubId).To(@event => @event.IdpSub))
        .From<UpstreamKeyDisplayIdentifierObserved>(from => from.Set(model => model.GitHubLogin).To(@event => @event.Login))
        .Children(model => model.LinkedUpstreamKeys, children => children
            .IdentifiedBy(key => key.LinkId)
            .From<UpstreamKeyLinkRecorded>(from => from.UsingKey(@event => @event.LinkId))
            .From<UpstreamKeySubjectRecorded>(from => from.UsingKey(@event => @event.LinkId))
            .From<UpstreamKeyDisplayIdentifierObserved>(from => from.UsingKey(@event => @event.LinkId))
            .RemovedWith<UpstreamKeyUnlinkRecorded>(removed => removed.UsingKey(@event => @event.LinkId)));
}

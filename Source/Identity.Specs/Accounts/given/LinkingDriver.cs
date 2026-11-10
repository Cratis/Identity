// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Validation;
using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Accounts.Linking.Proposing;
using Cratis.Identity.Accounts.Linking.Rejecting;
using Cratis.Identity.Accounts.Linking.Unlinking;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Configuration;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Upstream;
using Cratis.Monads;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Accounts.given;

/// <summary>
/// Drives the three native commands against guarded store state. Confirm and Clear stay on the old service until D3.
/// </summary>
internal class LinkingDriver(UpstreamKeyLinking linking, IEventStore eventStore, IReadModels readModels, UpstreamKeyDigester digester, IOptions<IdentityOptions> options, TimeProvider time)
{
    readonly AccountStore _store = new(eventStore, readModels, time);

    public async Task<LinkProposalId> Propose(ClaimsPrincipal session, VerifiedAuthentication authentication, LinkIntent intent)
    {
        var command = new ProposeLink();
        var read = new GuardedCommandRead(eventStore);
        var decision = Require(await command.Provide(Ceremony(session, authentication, intent), read.Decisions, readModels, new UpstreamLookups(readModels), digester, options, time));
        var (events, proposalId) = Require(command.Handle(decision));
        await Append(decision.Account, new(read.Guard, decision.State), events);
        return proposalId;
    }

    public Task<LinkId> Confirm(ClaimsPrincipal session, LinkProposalId proposalId, VerifiedAuthentication authentication) => linking.Confirm(session, proposalId, authentication);
    public Task Clear(ClaimsPrincipal resolver, AccountSubject account, LinkProposalId proposalId, VerifiedAuthentication authentication) => linking.Clear(resolver, account, proposalId, authentication);

    public async Task Reject(ClaimsPrincipal session, LinkProposalId proposalId)
    {
        var command = new RejectLink(proposalId);
        var read = new GuardedCommandRead(eventStore);
        var decision = Require(await command.Provide(Ceremony(session), read.Decisions, readModels, time));
        await Append(decision.Account, new(read.Guard, decision.State), Require(command.Handle(decision)));
    }

    public async Task Unlink(ClaimsPrincipal session, LinkId linkId)
    {
        var command = new UnlinkUpstreamKey(linkId);
        var read = new GuardedCommandRead(eventStore);
        var decision = Require(await command.Provide(Ceremony(session), read.Decisions, readModels, time));
        await Append(decision.Account, new(read.Guard, decision.State), Require(command.Handle(decision)));
    }

    static LinkCeremony Ceremony(ClaimsPrincipal session, VerifiedAuthentication? authentication = null, LinkIntent? intent = null)
    {
        var ceremony = new LinkCeremony();
        ceremony.Set(session, authentication, intent ?? new(false), CancellationToken.None);
        return ceremony;
    }

    static T Require<T>(Result<T, ValidationResult> result)
    {
        if (result.TryGetError(out var refusal))
        {
            throw new AccountOperationRefused(refusal.Message);
        }

        return result.AsT0;
    }

    Task Append(AccountSubject account, AccountStore.GuardedAccountRead read, IEnumerable<EventForEventSourceId> events) =>
        _store.Append(account, read, events.Select(item => item.Event));

    sealed class GuardedCommandRead
    {
        public GuardedCommandRead(IEventStore eventStore)
        {
            Decisions.Get<AccountGuard>(Arg.Any<ReadModelKey>()).Returns(async call =>
            {
                Guard = await eventStore.GetDecisionReads().GetDetached<AccountGuard>(call.Arg<ReadModelKey>());
                return Guard;
            });
        }

        public IDecisionReads Decisions { get; } = Substitute.For<IDecisionReads>();
        public DecisionRead<AccountGuard> Guard { get; private set; } = null!;
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Validation;
using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Accounts.Linking.Clearing;
using Cratis.Identity.Accounts.Linking.Confirming;
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

/// <summary>Drives native command providers and handlers through real guarded appends.</summary>
/// <param name="eventStore">The guarded event store.</param>
/// <param name="readModels">The passive account state and reservation reader.</param>
/// <param name="digester">The immutable upstream key digester.</param>
/// <param name="options">The realm policy.</param>
/// <param name="time">The trusted clock.</param>
/// <param name="refresh">Refreshes passive test projections before a bounded retry.</param>
internal class LinkingDriver(IEventStore eventStore, IReadModels readModels, UpstreamKeyDigester digester, IOptions<IdentityOptions> options, TimeProvider time, Func<Task> refresh)
{
    public async Task<LinkProposalId> Propose(ClaimsPrincipal session, VerifiedAuthentication authentication, LinkIntent intent)
    {
        var command = new ProposeLink();
        var read = new GuardedCommandRead(eventStore);
        var decision = Require(await command.Provide(Ceremony(session, authentication, intent), read.Decisions, new AccountReader(readModels), new UpstreamLookups(readModels), digester, options, time));
        var (events, proposalId) = Require(command.Handle(decision));
        await Append(read.Guard, events);
        return proposalId;
    }

    public Task<LinkId> Confirm(ClaimsPrincipal session, LinkProposalId proposalId, VerifiedAuthentication authentication) => Commit(Ceremony(session, authentication), proposalId, null);

    public async Task Clear(ClaimsPrincipal resolver, AccountSubject account, LinkProposalId proposalId, VerifiedAuthentication authentication) =>
        _ = await Commit(Ceremony(resolver, authentication), proposalId, account);

    public async Task Reject(ClaimsPrincipal session, LinkProposalId proposalId)
    {
        var command = new RejectLink(proposalId);
        var read = new GuardedCommandRead(eventStore);
        var decision = Require(await command.Provide(Ceremony(session), read.Decisions, new AccountReader(readModels), time));
        await Append(read.Guard, Require(command.Handle(decision)));
    }

    public async Task Unlink(ClaimsPrincipal session, LinkId linkId)
    {
        var command = new UnlinkUpstreamKey(linkId);
        var read = new GuardedCommandRead(eventStore);
        var decision = Require(await command.Provide(Ceremony(session), read.Decisions, new AccountReader(readModels), time));
        await Append(read.Guard, Require(command.Handle(decision)));
    }

    public async Task Append(AccountSubject account, IEnumerable<EventForEventSourceId> events) =>
        await Append(await eventStore.GetDecisionReads().GetDetached<AccountGuard>(new ReadModelKey(account.Value)), events);

    async Task<LinkId> Commit(LinkCeremony ceremony, LinkProposalId proposalId, AccountSubject? account)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var read = new GuardedCommandRead(eventStore);
            var (events, outcome) = await Decide(ceremony, proposalId, account, read.Decisions);
            var result = await eventStore.EventLog.AppendMany(events, [read.Guard]);
            if (result.IsSuccess)
            {
                return outcome.LinkId ?? throw new AccountOperationRefused("upstream_key_already_reserved");
            }

            if (attempt == 0)
            {
                await refresh();
                continue;
            }

            throw new AccountOperationRefused(result.HasConstraintViolations ? "upstream_key_already_reserved" : "account_concurrency_conflict");
        }

        throw new InvalidOperationException("Unreachable command attempt.");
    }

    async Task<(IEnumerable<EventForEventSourceId>, LinkConfirmationOutcome)> Decide(LinkCeremony ceremony, LinkProposalId proposalId, AccountSubject? account, IDecisionReads decisions)
    {
        if (account is not null)
        {
            var command = new ClearLinkQuarantine(account, proposalId);
            var decision = Require(await command.Provide(ceremony, decisions, new AccountReader(readModels), new UpstreamLookups(readModels), digester, options, time));
            return Require(command.Handle(decision));
        }

        var confirm = new ConfirmLink(proposalId);
        var pending = Require(await confirm.Provide(ceremony, decisions, new AccountReader(readModels), new UpstreamLookups(readModels), digester, options, time));
        return Require(confirm.Handle(pending));
    }

    async Task Append(DecisionRead<AccountGuard> guard, IEnumerable<EventForEventSourceId> events)
    {
        var result = await eventStore.EventLog.AppendMany(events, [guard]);
        if (!result.IsSuccess)
        {
            throw new AccountOperationRefused(result.HasConstraintViolations ? "upstream_key_already_reserved" : "account_concurrency_conflict");
        }
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

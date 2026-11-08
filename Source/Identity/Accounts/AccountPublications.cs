// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Accounts.Events;

namespace Cratis.Identity.Accounts;

/// <summary>Publishes pseudonymous contracts in source order; failure holds the reactor partition.</summary>
/// <param name="eventStore">The outbox owner.</param>
/// <param name="logger">Operational errors only.</param>
[Reactor("identity-account-publications")]
internal partial class AccountPublications(IEventStore eventStore, ILogger<AccountPublications> logger) : IReactor
{
    /// <summary>Publishes registration.</summary>
    /// <param name="event">The source fact.</param>
    /// <param name="context">The source identity.</param>
    /// <returns>The publication.</returns>
    public Task Publish(AccountRegistrationRecorded @event, EventContext context) => Publish(new AccountRegistered(@event.RegisteredAt), context, @event.FactId);

    /// <summary>Publishes disablement.</summary>
    /// <param name="event">The source fact.</param>
    /// <param name="context">The source identity.</param>
    /// <returns>The publication.</returns>
    public Task Publish(AccountDisablementRecorded @event, EventContext context) => Publish(new AccountDisabled(@event.ReasonId, @event.AccountEpoch, @event.FenceId, @event.FenceSequence), context, @event.FactId);

    /// <summary>Publishes enablement.</summary>
    /// <param name="event">The source fact.</param>
    /// <param name="context">The source identity.</param>
    /// <returns>The publication.</returns>
    public Task Publish(AccountEnablementRecorded @event, EventContext context) => Publish(new AccountEnabled(@event.AccountEpoch, @event.EnabledAt), context, @event.FactId);

    /// <summary>Publishes tombstoning.</summary>
    /// <param name="event">The source fact.</param>
    /// <param name="context">The source identity.</param>
    /// <returns>The publication.</returns>
    public Task Publish(AccountTombstoneRecorded @event, EventContext context) => Publish(new AccountTombstoned(@event.TombstonedAt), context, @event.FactId);

    /// <summary>Publishes completed erasure.</summary>
    /// <param name="event">The source fact.</param>
    /// <param name="context">The source identity.</param>
    /// <returns>The publication.</returns>
    public Task Publish(AccountErasureRecorded @event, EventContext context) => Publish(new AccountErased(@event.ErasureId, @event.CompletedAt, @event.BoundaryVersion), context, @event.FactId);

    /// <summary>Publishes linking, never raw upstream subject or display identifier.</summary>
    /// <param name="event">The source fact.</param>
    /// <param name="context">The source identity.</param>
    /// <returns>The publication.</returns>
    public Task Publish(UpstreamKeyLinkRecorded @event, EventContext context) => Publish(new UpstreamKeyLinked(@event.LinkId, @event.ConnectionId, @event.IdpIssuer, @event.KeyHash, @event.EvidenceId, @event.LinkedAt), context, @event.FactId);

    /// <summary>Publishes unlinking without releasing a reservation.</summary>
    /// <param name="event">The source fact.</param>
    /// <param name="context">The source identity.</param>
    /// <returns>The publication.</returns>
    public Task Publish(UpstreamKeyUnlinkRecorded @event, EventContext context) => Publish(new UpstreamKeyUnlinked(@event.LinkId, @event.KeyHash, @event.ReasonId, @event.UnlinkedAt), context, @event.FactId);

    [LoggerMessage(LogLevel.Error, "Account publication held for event source {EventSource}, source sequence {Sequence}")]
    static partial void PublicationHeld(ILogger logger, string eventSource, ulong sequence, Exception exception);

    async Task Publish(object contract, EventContext context, FactId factId)
    {
        var type = eventStore.EventTypes.GetEventTypeFor(contract.GetType());
        var identity = $"{factId.Value:D}:{type.Id.Value}";
        try
        {
            // The durable idempotency key is also exposed as a non-PII tag for consumer deduplication.
            // One reactor observes all six source types. Throwing suspends this source partition, never skips it.
            var result = await eventStore.GetEventSequence(EventSequenceId.Outbox).AppendOnce(
                context.EventSourceId,
                contract,
                new IdempotencyKey(identity),
                correlationId: context.CorrelationId,
                tags: [$"event-identity:{identity}"],
                occurred: context.Occurred);
            if (!result.IsSuccess)
            {
                throw new AccountOperationRefused("account_publication_failed");
            }
        }
        catch (Exception exception)
        {
            PublicationHeld(logger, context.EventSourceId.Value, context.SequenceNumber.Value, exception);
            throw;
        }
    }
}

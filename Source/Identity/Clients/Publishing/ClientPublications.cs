// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
using Cratis.Identity.Contracts.Clients.Events;

namespace Cratis.Identity.Clients.Publishing;

/// <summary>
/// Publishes the Public client contracts to the outbox in source order, on the client's own stream (its <see cref="Contracts.Clients.ClientRegistrationId"/>).
/// A client is added once and its secret rotates separately; the secret digest stays in the Internal fact and never reaches a contract.
/// Failure holds the reactor partition rather than skipping a fact.
/// </summary>
/// <remarks>
/// The released client facts carry no fact identity, so each publication is keyed by what makes it unique: a client is registered and retired
/// once, a secret rotation has its own revision reference, and a redirect change is keyed by when it happened and what it set. Nothing depends on
/// the source sequence number, which does not survive a restore.
/// </remarks>
/// <param name="eventStore">The outbox owner.</param>
/// <param name="logger">Operational errors only.</param>
[Reactor("identity-client-publications")]
internal partial class ClientPublications(IEventStore eventStore, ILogger<ClientPublications> logger) : IReactor
{
    /// <summary>Publishes the client's registration, without its redirect URIs and secret, which are their own contracts.</summary>
    /// <param name="event">The source fact.</param>
    /// <param name="context">The source identity.</param>
    /// <returns>The publication.</returns>
    public Task Publish(ClientRegistered @event, EventContext context) => Publish(
        new ClientAdded(
            @event.ClientId,
            @event.RegistrationKind,
            @event.RegisteredAt,
            @event.ClientType,
            @event.ApplicationType,
            @event.RequirePkce,
            @event.ConsentType,
            @event.Endpoints,
            @event.GrantTypes,
            @event.ResponseTypes,
            @event.ResourceIndicators,
            @event.Scopes),
        context,
        "once");

    /// <summary>Publishes the redirect allowlist.</summary>
    /// <param name="event">The source fact.</param>
    /// <param name="context">The source identity.</param>
    /// <returns>The publication.</returns>
    public Task Publish(ClientRedirectUrisChanged @event, EventContext context) =>
        Publish(new RedirectUrisChangedForClient(@event.RedirectUris), context, $"{context.Occurred.UtcTicks.ToString(CultureInfo.InvariantCulture)}:{Digest(@event.RedirectUris)}");

    /// <summary>Publishes a secret rotation without the keyed digest.</summary>
    /// <param name="event">The source fact.</param>
    /// <param name="context">The source identity.</param>
    /// <returns>The publication.</returns>
    public Task Publish(ClientSecretRotated @event, EventContext context) =>
        Publish(new SecretRotatedForClient(@event.ClientSecretId, @event.ActivatedAt, @event.PreviousValidUntil), context, @event.ClientSecretId.Value.ToString("D"));

    /// <summary>Publishes retirement.</summary>
    /// <param name="event">The source fact.</param>
    /// <param name="context">The source identity.</param>
    /// <returns>The publication.</returns>
    public Task Publish(ClientRetired @event, EventContext context) => Publish(new ClientPermanentlyRetired(@event.ReasonId, @event.RetiredAt), context, "once");

    [LoggerMessage(LogLevel.Error, "Client publication held for event source {EventSource}, source sequence {Sequence}")]
    static partial void PublicationHeld(ILogger logger, string eventSource, ulong sequence, Exception exception);

    static string Digest(IEnumerable<string> values) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', values))));

    async Task Publish(object contract, EventContext context, string discriminator)
    {
        var type = eventStore.EventTypes.GetEventTypeFor(contract.GetType());
        var identity = $"{context.EventSourceId.Value}:{type.Id.Value}:{discriminator}";
        try
        {
            // The durable idempotency key is also exposed as a non-PII tag for consumer deduplication.
            var result = await eventStore.GetEventSequence(EventSequenceId.Outbox).AppendOnce(
                context.EventSourceId,
                contract,
                new IdempotencyKey(identity),
                correlationId: context.CorrelationId,
                tags: [$"event-identity:{identity}"],
                occurred: context.Occurred);
            if (!result.IsSuccess)
            {
                throw new ClientPublicationRefused("client_publication_failed");
            }
        }
        catch (Exception exception)
        {
            PublicationHeld(logger, context.EventSourceId.Value, context.SequenceNumber.Value, exception);
            throw;
        }
    }
}

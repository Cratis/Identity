// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Configuration;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;
using Cratis.Identity.Resources;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Clients.Requests;

/// <summary>
/// Carries out the client requests a product publishes. When <c language="text">Identity:ClientRequests:SourceStore</c> names the product's event
/// store, this subscribes to that store's outbox and acts on <see cref="ClientRegistrationRequested"/> and
/// <see cref="ClientRetirementRequested"/> through <see cref="ClientRequestDecisions"/>, so no service writes into another's store.
/// </summary>
/// <remarks>
/// The store name is configuration, not a constant, because one Cratis Identity image serves several products, so the observer is
/// registered at startup instead of being discovered. A refused request is logged and acknowledged: refusing it again would never
/// change the answer. Redelivery is harmless because a registration is refused once its client id exists.
/// </remarks>
/// <param name="configuration">The configuration holding the source store name.</param>
/// <param name="eventStore">Cratis Identity's own event store.</param>
/// <param name="services">Resolves what reads the registrations when a request is handled.</param>
/// <param name="options">The Cratis Identity options holding the security owners.</param>
/// <param name="descriptors">The configured clients, whose ids cannot be registered at runtime.</param>
/// <param name="resources">The resources and scopes Cratis Identity serves.</param>
/// <param name="logger">The logger.</param>
internal sealed partial class ProductClientRequests(
    IConfiguration configuration,
    IEventStore eventStore,
    IServiceProvider services,
    IOptions<IdentityOptions> options,
    ClientDescriptors descriptors,
    IResourceCatalog resources,
    ILogger<ProductClientRequests> logger) : IHostedService
{
    /// <summary>The configuration key naming the product's event store.</summary>
    public const string SourceStoreKey = "Identity:ClientRequests:SourceStore";

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var source = configuration[SourceStoreKey];
        if (string.IsNullOrWhiteSpace(source))
        {
            return;
        }

        await eventStore.Subscriptions.Subscribe(
            $"identity-client-requests-{source}",
            source,
            subscription => subscription.WithEventType<ClientRegistrationRequested>().WithEventType<ClientRetirementRequested>());

        await eventStore.Reactors.Register(
            "identity-client-requests",
            reactor => reactor
                .OnEventSequence(EventSequenceId.InboxPrefix + source)
                .On<ClientRegistrationRequested>((request, context) => Register(request, context))
                .On<ClientRetirementRequested>((request, context) => Retire(request, context)));
        LogListening(source);
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    async Task Register(ClientRegistrationRequested request, EventContext context)
    {
        var reserved = descriptors.All().Select(descriptor => descriptor.ClientId).OfType<string>().ToHashSet(StringComparer.Ordinal);
        var scopes = resources.All.SelectMany(resource => resource.Scopes).Select(scope => scope.Name).ToHashSet(StringComparer.Ordinal);
        var outcome = ClientRequestDecisions.ForRegistration(
            request,
            [.. options.Value.SecurityOwners],
            reserved,
            scopes,
            ClientRegistrationId.New(),
            (services.GetService<TimeProvider>() ?? TimeProvider.System).GetUtcNow());
        await Apply(outcome, "registration", request.ClientId, context);
    }

    async Task Retire(ClientRetirementRequested request, EventContext context)
    {
        await using var scope = services.CreateAsyncScope();
        var registrations = await scope.ServiceProvider.GetRequiredService<IReadModels>().GetInstances<ClientRegistration>();
        var registration = registrations.FirstOrDefault(candidate => candidate.Id == request.RegistrationId.Value);
        var outcome = ClientRequestDecisions.ForRetirement(
            request,
            [.. options.Value.SecurityOwners],
            registration,
            (services.GetService<TimeProvider>() ?? TimeProvider.System).GetUtcNow());
        await Apply(outcome, "retirement", request.RegistrationId.Value.ToString(), context);
    }

    async Task Apply(ClientRequestOutcome outcome, string kind, string subject, EventContext context)
    {
        if (!outcome.Accepted)
        {
            LogRefused(kind, subject, outcome.Refusal!, context.SequenceNumber.Value);
            return;
        }

        var result = await eventStore.GetEventSequence(EventSequenceId.Log).AppendMany(outcome.Events);
        if (!result.IsSuccess)
        {
            // A client id that already exists is the expected redelivery; anything else is retried by Chronicle.
            LogNotAppended(kind, subject, context.SequenceNumber.Value);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Listening for client requests from the '{Store}' event store.")]
    partial void LogListening(string store);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Refused a client {Kind} request for '{Subject}' ({Reason}, inbox sequence number {SequenceNumber}).")]
    partial void LogRefused(string kind, string subject, string reason, ulong sequenceNumber);

    [LoggerMessage(Level = LogLevel.Warning, Message = "A client {Kind} request for '{Subject}' appended nothing (inbox sequence number {SequenceNumber}); it may already have been carried out.")]
    partial void LogNotAppended(string kind, string subject, ulong sequenceNumber);
}

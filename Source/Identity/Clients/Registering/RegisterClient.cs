// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.EventSequences;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;
using Cratis.Identity.Hosting;
using Cratis.Monads;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Identity.Clients.Registering;

/// <summary>Registers an owner-approved client; plaintext lives only on this command.</summary>
/// <param name="ClientId">The protocol identifier to reserve permanently.</param>
/// <param name="Configuration">Non-secret accepted settings.</param>
/// <param name="Secret">The initial confidential secret, never an event field.</param>
[Command, ExcludeFromDiscovery]
[Authorize(Policy = IdentityPolicies.IssuerSecurityOwner)]
internal sealed record RegisterClient(ClientId ClientId, ClientConfiguration Configuration, [property: NotAudited] string? Secret = null)
{
    /// <summary>Reads the trusted registration time and configured identifier reservations.</summary>
    /// <param name="reads">The trusted clock and configured-identifier decision reads.</param>
    /// <returns>The trusted registration decision context.</returns>
    public ClientRegistrationDecision Provide(ClientRegistrationReads reads) => reads.Read(ClientId);

    /// <summary>Returns one atomic registration batch, including the initial secret digest when confidential.</summary>
    /// <param name="decision">The trusted registration time and configured reservations.</param>
    /// <param name="digester">The server-keyed digest producer.</param>
    /// <returns>Internal facts, or a credential-free refusal.</returns>
    public Result<IEnumerable<EventForEventSourceId>, ValidationResult> Handle(ClientRegistrationDecision decision, ClientSecretDigester digester)
    {
        if (!ClientIdRules.IsValid(ClientId))
        {
            return ValidationResult.Error("invalid_client_registration");
        }

        if (decision.ConfiguredClientExists)
        {
            return ValidationResult.Error("client_id_already_reserved");
        }

        if (Configuration.Validate(Secret) is { } refusal)
        {
            return refusal;
        }

        var id = ClientRegistrationId.New();
        var events = new List<EventForEventSourceId>
        {
            new(id.Value, Configuration.Registered(ClientId, decision.At)),
            new(id.Value, new ClientRedirectUrisChanged(Configuration.RedirectUris.ToArray()))
        };
        if (Configuration.ClientType == ClientType.Confidential)
        {
            events.Add(new(id.Value, new ClientSecretRotated(ClientSecretId.New(), digester.Digest(Secret!), decision.At, decision.At)));
        }

        return events;
    }

    /// <inheritdoc/>
    public override string ToString() => "Register client [redacted]";
}

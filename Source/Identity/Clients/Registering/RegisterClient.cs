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
    /// <summary>Provides a trusted registration instant.</summary>
    /// <param name="time">The server clock.</param>
    /// <returns>The registration time.</returns>
    public DateTimeOffset Provide(TimeProvider time) => time.GetUtcNow();

    /// <summary>Returns one atomic registration batch, including the initial secret digest when confidential.</summary>
    /// <param name="at">The trusted registration time.</param>
    /// <param name="digester">The server-keyed digest producer.</param>
    /// <returns>Internal facts, or a credential-free refusal.</returns>
    public Result<IEnumerable<EventForEventSourceId>, ValidationResult> Handle(DateTimeOffset at, ClientSecretDigester digester)
    {
        if (string.IsNullOrWhiteSpace(ClientId.Value))
        {
            return ValidationResult.Error("invalid_client_registration");
        }

        if (Configuration.Validate(Secret) is { } refusal)
        {
            return refusal;
        }

        var id = ClientRegistrationId.New();
        var events = new List<EventForEventSourceId>
        {
            new(id.Value, Configuration.Registered(ClientId, at)),
            new(id.Value, new ClientRedirectUrisChanged(Configuration.RedirectUris.ToArray()))
        };
        if (Configuration.ClientType == ClientType.Confidential)
        {
            events.Add(new(id.Value, new ClientSecretRotated(ClientSecretId.New(), digester.Digest(Secret!), at, at)));
        }

        return events;
    }

    /// <inheritdoc/>
    public override string ToString() => "Register client [redacted]";
}

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
    /// <summary>Returns one atomic registration batch, including the initial secret digest when confidential.</summary>
    /// <param name="descriptors">The configured identifiers, independent of Internal registration streams.</param>
    /// <param name="time">The server registration clock.</param>
    /// <param name="digester">The server-keyed digest producer.</param>
    /// <returns>Internal facts, or a credential-free refusal.</returns>
    public Result<IEnumerable<EventForEventSourceId>, ValidationResult> Handle(ClientDescriptors descriptors, TimeProvider time, ClientSecretDigester digester)
    {
        if (!IsValidClientId(ClientId))
        {
            return ValidationResult.Error("invalid_client_registration");
        }

        if (descriptors.All().Any(descriptor => string.Equals(descriptor.ClientId, ClientId?.Value, StringComparison.Ordinal)))
        {
            return ValidationResult.Error("client_id_already_reserved");
        }

        if (Configuration.Validate(Secret) is { } refusal)
        {
            return refusal;
        }

        var at = time.GetUtcNow();
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

    /// <summary>D3 protocol identifier admission: 1..128 RFC 6749 VSCHAR characters, excluding colon and edge whitespace, without normalization or case folding.</summary>
    /// <param name="id">The proposed exact identifier.</param>
    /// <returns>Whether the identifier is admitted.</returns>
    static bool IsValidClientId(ClientId? id)
    {
        var value = id?.Value;
        if (value is not { Length: > 0 and <= 128 })
        {
            return false;
        }

        return value[0] != ' ' && value[^1] != ' ' && value.All(character => character is >= ' ' and <= '~' and not ':');
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Keys;
using Cratis.Identity.Contracts.Clients;
using Cratis.Identity.Contracts.Clients.Events;
using Cratis.Identity.Hosting;
using Cratis.Monads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Clients.RotatingSecret;

/// <summary>Rotates a confidential client secret without persisting plaintext.</summary>
/// <param name="RegistrationId">The opaque stream identity.</param>
/// <param name="Secret">The command-only replacement secret.</param>
/// <param name="Window">The activation and overlap window.</param>
[Command, ExcludeFromDiscovery, ReadProtection(ReadProtectionMode.On)]
[Authorize(Policy = IdentityPolicies.IssuerSecurityOwner)]
internal sealed record RotateSecret([property: Key] ClientRegistrationId RegistrationId, [property: NotAudited] string Secret, SecretRotationWindow Window)
{
    /// <summary>Returns a keyed digest fact, never the plaintext command.</summary>
    /// <param name="state">The guarded registration.</param>
    /// <param name="digester">The server-keyed producer.</param>
    /// <param name="time">The trusted server clock.</param>
    /// <param name="options">The server's overlap policy.</param>
    /// <returns>The Internal fact, or a refusal.</returns>
    public Result<IEnumerable<EventForEventSourceId>, ValidationResult> Handle([LookUpBy(nameof(RegistrationId))] ClientRegistration? state, ClientSecretDigester digester, TimeProvider time, IOptions<ClientSecretOptions> options)
    {
        if (ClientRegistration.RequireActive(state) is { } refusal)
        {
            return refusal;
        }

        if (state!.Configuration.ClientType != ClientType.Confidential || !ClientSecretStrength.IsValid(Secret))
        {
            return ValidationResult.Error("invalid_client_secret_rotation");
        }

        if (!Window.IsValidAfter(state.ActivatedAt, time.GetUtcNow(), options.Value.MaxOverlap))
        {
            return ValidationResult.Error("invalid_client_secret_deadlines");
        }

        var digest = digester.Digest(Secret);
        if (digest == state.SecretHash || state.ValidSecretHashesAt(time.GetUtcNow()).Contains(digest))
        {
            return ValidationResult.Error("client_secret_unchanged");
        }

        return new EventForEventSourceId[]
        {
            new(RegistrationId.Value, new ClientSecretRotated(ClientSecretId.New(), digest, Window.ActivatedAt, Window.PreviousValidUntil))
        };
    }

    /// <inheritdoc/>
    public override string ToString() => "Rotate client secret [redacted]";
}

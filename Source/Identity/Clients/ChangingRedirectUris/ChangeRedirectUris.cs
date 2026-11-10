// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
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

namespace Cratis.Identity.Clients.ChangingRedirectUris;

/// <summary>Replaces the exact redirect allowlist of an active client.</summary>
/// <param name="RegistrationId">The opaque stream identity.</param>
/// <param name="RedirectUris">The new exact allowlist.</param>
[Command, ExcludeFromDiscovery, ReadProtection(ReadProtectionMode.On)]
[Authorize(Policy = IdentityPolicies.IssuerSecurityOwner)]
internal sealed record ChangeRedirectUris([property: Key] ClientRegistrationId RegistrationId, string[] RedirectUris)
{
    /// <summary>Validates and returns a redirect fact without appending.</summary>
    /// <param name="state">The guarded registration.</param>
    /// <returns>The Internal fact, or a refusal.</returns>
    public Result<IEnumerable<EventForEventSourceId>, ValidationResult> Handle([LookUpBy(nameof(RegistrationId))] ClientRegistration? state)
    {
        if (ClientDecisions.RequireActive(state) is { } refusal)
        {
            return refusal;
        }

        var configuration = state!.Configuration with { RedirectUris = RedirectUris };
        if (configuration.Validate(state.SecretHash is null ? null : "existing-secret") is { } invalid)
        {
            return invalid;
        }

        return new EventForEventSourceId[] { new(RegistrationId.Value, new ClientRedirectUrisChanged(RedirectUris.ToArray())) };
    }
}

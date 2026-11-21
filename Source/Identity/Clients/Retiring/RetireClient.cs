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

namespace Cratis.Identity.Clients.Retiring;

/// <summary>Permanently retires an owner-selected client without freeing its protocol identifier.</summary>
/// <param name="RegistrationId">The opaque stream identity.</param>
[Command, ExcludeFromDiscovery, ReadProtection(ReadProtectionMode.On)]
[Authorize(Policy = IdentityPolicies.IssuerSecurityOwner)]
internal sealed record RetireClient([property: Key] ClientRegistrationId RegistrationId)
{
    /// <summary>Returns a retirement fact with a closed reason, never caller text.</summary>
    /// <param name="state">The guarded registration.</param>
    /// <param name="time">The trusted server clock.</param>
    /// <returns>The Internal fact, or a refusal.</returns>
    public Result<IEnumerable<EventForEventSourceId>, ValidationResult> Handle([LookUpBy(nameof(RegistrationId))] ClientRegistration? state, TimeProvider time)
    {
        if (ClientRegistration.RequireActive(state) is { } refusal)
        {
            return refusal;
        }

        return new EventForEventSourceId[] { new(RegistrationId.Value, new ClientRetired("owner_retired", time.GetUtcNow())) };
    }
}

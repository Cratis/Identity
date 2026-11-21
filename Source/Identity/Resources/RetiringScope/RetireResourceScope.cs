// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Keys;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Contracts.Resources;
using Cratis.Identity.Contracts.Resources.Events;
using Cratis.Identity.Hosting;
using Cratis.Monads;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Identity.Resources.RetiringScope;

/// <summary>Permanently retires a scope, without releasing its reservation.</summary>
/// <param name="ResourceId">The registered resource stream identity.</param>
/// <param name="ScopeName">The exact registered scope.</param>
[Command, ExcludeFromDiscovery, ReadProtection(ReadProtectionMode.On)]
[Authorize(Policy = IdentityPolicies.IssuerSecurityOwner)]
internal sealed record RetireResourceScope([property: Key] ResourceId ResourceId, string ScopeName)
{
    /// <summary>Returns a source fact using a bounded, server-held retirement instant.</summary>
    /// <param name="state">The guarded registration.</param>
    /// <param name="time">The server clock, never caller input.</param>
    /// <returns>The Internal source fact, or a closed refusal.</returns>
    public Result<IEnumerable<EventForEventSourceId>, ValidationResult> Handle([LookUpBy(nameof(ResourceId))] ResourceRegistration? state, TimeProvider time)
    {
        if (ResourceRegistration.RequireRegistered(ResourceId, state) is { } refusal)
        {
            return refusal;
        }

        if (!ResourceRegistrationValidation.IsScope(ScopeName, state!.ScopePrefix))
        {
            return ValidationResult.Error("invalid_resource_scope");
        }

        var scope = state.Scopes.SingleOrDefault(scope => scope.ScopeName == ScopeName);
        if (scope?.Retired != false)
        {
            return ValidationResult.Error("resource_scope_not_active");
        }

        var at = time.GetUtcNow();
        if (at < DateTimeOffset.UnixEpoch || at == DateTimeOffset.MaxValue)
        {
            return ValidationResult.Error("invalid_resource_retirement_time");
        }

        return new EventForEventSourceId[] { new(ResourceId.Value, new ResourceScopeRetirementRecorded(ScopeName, at, FactId.New())) };
    }
}

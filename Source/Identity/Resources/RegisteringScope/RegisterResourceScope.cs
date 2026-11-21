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
using Cratis.Identity.Resources.Registering;
using Cratis.Monads;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Identity.Resources.RegisteringScope;

/// <summary>Registers a validated scope on a guarded, registered resource stream.</summary>
/// <param name="ResourceId">The resource stream identity.</param>
/// <param name="ScopeName">The exact prefixed scope name.</param>
/// <param name="Description">The Internal business description.</param>
[Command, ExcludeFromDiscovery, ReadProtection(ReadProtectionMode.On)]
[Authorize(Policy = IdentityPolicies.IssuerSecurityOwner)]
internal sealed record RegisterResourceScope([property: Key] ResourceId ResourceId, string ScopeName, ResourceScopeDescription Description)
{
    /// <summary>Appends an Internal scope source fact, without reviving an existing reservation.</summary>
    /// <param name="state">The guarded resource registration.</param>
    /// <param name="catalog">The trusted configured catalogue.</param>
    /// <returns>The source fact, or a closed refusal.</returns>
    public Result<IEnumerable<EventForEventSourceId>, ValidationResult> Handle([LookUpBy(nameof(ResourceId))] ResourceRegistration? state, IResourceCatalog catalog)
    {
        if (ResourceRegistration.RequireRegistered(ResourceId, state) is { } refusal)
        {
            return refusal;
        }

        if (!ResourceRegistrationValidation.IsScope(ScopeName, state!.ScopePrefix) || !ResourceRegistrationValidation.IsLabel(Description?.Value))
        {
            return ValidationResult.Error("invalid_resource_scope");
        }

        if (RegisterResource.Conflicts(catalog.All, state.ResourceIndicator, state.Audience, state.ScopePrefix) || state.Scopes.Any(scope => scope.ScopeName == ScopeName))
        {
            return ValidationResult.Error("resource_scope_already_reserved");
        }

        return new EventForEventSourceId[] { new(ResourceId.Value, new ResourceScopeRegistrationRecorded(ScopeName, Description!, FactId.New())) };
    }
}

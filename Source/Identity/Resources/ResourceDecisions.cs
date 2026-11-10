// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Identity.Contracts.Resources;

namespace Cratis.Identity.Resources;

/// <summary>Fail-closed guarded resource identity checks.</summary>
internal static class ResourceDecisions
{
    /// <summary>Requires the selected stream's registration, not caller-supplied state.</summary>
    /// <param name="id">The selected resource.</param>
    /// <param name="state">The guarded decision state.</param>
    /// <returns>A closed refusal, or null.</returns>
    public static ValidationResult? RequireRegistered(ResourceId? id, ResourceRegistration? state)
    {
        if (id is null || id.Value == Guid.Empty)
        {
            return ValidationResult.Error("invalid_resource_id");
        }

        return state is null || state.Id != id || string.IsNullOrEmpty(state.ResourceIndicator) ?
            ValidationResult.Error("resource_not_registered") : null;
    }
}

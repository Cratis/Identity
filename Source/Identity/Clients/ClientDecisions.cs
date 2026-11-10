// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Identity.Clients;

/// <summary>Shared fail-closed client lifecycle checks.</summary>
internal static class ClientDecisions
{
    /// <summary>Requires an existing, non-retired registration.</summary>
    /// <param name="state">The guarded state.</param>
    /// <returns>A credential-free refusal, or null.</returns>
    public static ValidationResult? RequireActive(ClientRegistration? state)
    {
        if (state?.ClientId is null)
        {
            return ValidationResult.Error("client_not_registered");
        }

        return state.Retired ? ValidationResult.Error("client_retired") : null;
    }
}

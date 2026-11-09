// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Clients;

/// <summary>
/// Describes whether a client registration satisfies the registration rules.
/// </summary>
/// <param name="Refusals">The non-secret reasons for refusing the registration.</param>
internal sealed record ClientRegistrationValidation(IReadOnlyList<string> Refusals)
{
    /// <summary>
    /// Gets whether the registration is valid.
    /// </summary>
    public bool IsValid => Refusals.Count == 0;
}

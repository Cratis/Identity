// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts;

/// <summary>
/// Represents the GitHub user an upstream sign-in authenticated.
/// </summary>
/// <param name="Id">The numeric GitHub user identifier. Stable across login renames.</param>
/// <param name="Login">The GitHub login.</param>
/// <param name="Name">The display name, or the login when the profile has none.</param>
public record GitHubUser(long Id, string Login, string Name)
{
    /// <summary>Gets the optional public email observation, never identity or authorization evidence.</summary>
    public Email? Email { get; init; }
}

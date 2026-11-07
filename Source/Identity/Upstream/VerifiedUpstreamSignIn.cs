// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;

namespace Cratis.Identity.Upstream;

/// <summary>
/// Holds protocol-verified observations once within a command's child scope.
/// </summary>
internal sealed class VerifiedUpstreamSignIn : IVerifiedUpstreamSignIn
{
    /// <inheritdoc/>
    public VerifiedAuthentication? Authentication { get; private set; }

    /// <inheritdoc/>
    public ObservedProfile? Profile { get; private set; }

    /// <summary>
    /// Supplies the observations from the trusted protocol adapter exactly once.
    /// </summary>
    /// <param name="authentication">The verified authentication.</param>
    /// <param name="profile">The observed profile.</param>
    /// <exception cref="AccountOperationRefused">The scope already contains verified observations.</exception>
    internal void Set(VerifiedAuthentication authentication, ObservedProfile profile)
    {
        if (Authentication is not null)
        {
            throw new AccountOperationRefused("verified_sign_in_already_supplied");
        }

        Authentication = authentication;
        Profile = profile;
    }
}

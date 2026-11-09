// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;

namespace Cratis.Identity.Upstream;

/// <summary>Admits verified upstream authentication before constructing a session.</summary>
internal interface IAccountSignIn
{
    /// <summary>Signs in with trusted protocol metadata, never raw command payload credentials.</summary>
    /// <param name="authentication">The verified authentication.</param>
    /// <param name="profile">The observed profile.</param>
    /// <param name="cancellationToken">The request cancellation, observed while waiting for read-model catch-up.</param>
    /// <returns>The active account after the decision completes.</returns>
    Task<Account> SignIn(VerifiedAuthentication authentication, ObservedProfile profile, CancellationToken cancellationToken);
}

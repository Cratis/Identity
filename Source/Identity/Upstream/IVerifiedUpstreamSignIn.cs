// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;

namespace Cratis.Identity.Upstream;

/// <summary>
/// Provides the trusted, request-scoped sign-in observations to the protocol command.
/// </summary>
internal interface IVerifiedUpstreamSignIn
{
    /// <summary>
    /// Gets the verified upstream authentication, if supplied.
    /// </summary>
    VerifiedAuthentication? Authentication { get; }

    /// <summary>
    /// Gets the observed profile, if supplied.
    /// </summary>
    ObservedProfile? Profile { get; }

    /// <summary>
    /// Gets the cancellation of the originating request, observed while waiting for read-model catch-up.
    /// </summary>
    CancellationToken CancellationToken { get; }
}

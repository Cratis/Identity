// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts.Linking;

/// <summary>
/// Server-supplied observations for one link ceremony, never command properties.
/// </summary>
internal interface ILinkCeremony
{
    /// <summary>
    /// Gets the existing authenticated account principal.
    /// </summary>
    ClaimsPrincipal Session { get; }

    /// <summary>
    /// Gets the adapter-verified upstream proof, when required by the ceremony.
    /// </summary>
    VerifiedAuthentication? Authentication { get; }

    /// <summary>
    /// Gets the explicit ceremony intent and trusted connection policy.
    /// </summary>
    LinkIntent Intent { get; }

    /// <summary>
    /// Gets the originating request's cancellation.
    /// </summary>
    CancellationToken CancellationToken { get; }
}

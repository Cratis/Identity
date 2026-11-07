// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization;

/// <summary>
/// Holds the authentication scheme names Identity registers.
/// </summary>
internal static class IdentitySchemes
{
    /// <summary>
    /// The cookie scheme for the Identity site session. It is only ever read by Identity itself.
    /// </summary>
    public const string Session = "Identity.Session";

    /// <summary>
    /// The GitHub OAuth scheme used as upstream login.
    /// </summary>
    public const string GitHub = "GitHub";
}

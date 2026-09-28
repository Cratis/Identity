// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Authorization;

/// <summary>
/// Holds the claim types Identity writes beyond the registered JWT claims.
/// </summary>
public static class IdentityClaims
{
    /// <summary>
    /// The numeric GitHub user identifier of the account.
    /// </summary>
    public const string GitHubId = "github_id";

    /// <summary>
    /// The GitHub login of the account at the time the token was issued.
    /// </summary>
    public const string GitHubLogin = "github_login";

    /// <summary>
    /// The tenant the user selected when authorizing the client.
    /// </summary>
    public const string TenantId = "tid";
}

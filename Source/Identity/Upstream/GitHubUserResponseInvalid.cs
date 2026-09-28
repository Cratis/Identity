// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Upstream;

/// <summary>
/// The exception that is thrown when GitHub answers the user endpoint without a numeric id or a login, so the
/// sign-in cannot be linked to an account.
/// </summary>
public class GitHubUserResponseInvalid()
    : Exception("GitHub returned a user without a numeric id or a login; the sign-in cannot be linked to an account.");

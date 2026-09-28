// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts;

/// <summary>
/// Represents a Cratis account.
/// </summary>
/// <param name="Id">The canonical account identifier, written to the <c language="text">sub</c> claim.</param>
/// <param name="GitHubId">The numeric GitHub user identifier the account is linked to.</param>
/// <param name="GitHubLogin">The GitHub login at the most recent sign-in.</param>
/// <param name="Name">The display name at the most recent sign-in.</param>
public record Account(string Id, long GitHubId, string GitHubLogin, string Name);

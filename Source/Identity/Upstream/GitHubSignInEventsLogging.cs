// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Upstream;

/// <summary>
/// Holds log messages for <see cref="GitHubSignInEvents"/>.
/// </summary>
internal static partial class GitHubSignInEventsLogging
{
    [LoggerMessage(LogLevel.Information, "Account {AccountId} signed in with GitHub")]
    internal static partial void SignedIn(this ILogger<GitHubSignInEvents> logger, string accountId);

    [LoggerMessage(LogLevel.Warning, "GitHub sign-in failed")]
    internal static partial void SignInFailed(this ILogger<GitHubSignInEvents> logger, Exception? failure);
}

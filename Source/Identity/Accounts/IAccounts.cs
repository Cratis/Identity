// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts;

/// <summary>
/// Defines the store of Cratis accounts.
/// </summary>
public interface IAccounts
{
    /// <summary>
    /// Records a GitHub sign-in, creating the account the first time the GitHub user signs in.
    /// </summary>
    /// <param name="user">The authenticated <see cref="GitHubUser"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Account"/> linked to the GitHub user.</returns>
    Task<Account> RecordGitHubSignIn(GitHubUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an account by its canonical identifier.
    /// </summary>
    /// <param name="id">The account identifier.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Account"/>, or <see langword="null"/> if it does not exist.</returns>
    Task<Account?> FindById(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Erases the personal fields of an account: removes its stored document and leaves only an erasure marker.
    /// Erasing an account that is already erased or unknown succeeds without effect.
    /// </summary>
    /// <param name="id">The account identifier.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Erase(string id, CancellationToken cancellationToken = default);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Cratis.Identity.Accounts.Linking;
using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts;

/// <summary>
/// Reads passive account views. A deciding command must separately enroll its source guard and await projection progress.
/// </summary>
/// <param name="readModels">The materialized projection reader.</param>
internal class AccountReader(IReadModels readModels) : IAccountReader
{
    /// <summary>
    /// Reads the owner's account without granting permission to modify it or create a session.
    /// </summary>
    /// <param name="subject">The immutable account subject.</param>
    /// <returns>The account, if present.</returns>
    public async Task<Account?> Find(AccountSubject subject) => await readModels.GetInstanceById<Account>(subject.Value);

    /// <summary>
    /// Reads passive link state after the deciding command has enrolled the same account's guard.
    /// </summary>
    /// <param name="subject">The guarded account subject.</param>
    /// <returns>The account link state, if present.</returns>
    public async Task<AccountLinkState?> FindLinkState(AccountSubject subject) => await readModels.GetInstanceById<AccountLinkState>(subject.Value);

    /// <summary>
    /// Reads an active account for session creation after command completion and projection catch-up.
    /// </summary>
    /// <param name="subject">The immutable account subject.</param>
    /// <param name="cancellationToken">The request cancellation.</param>
    /// <returns>The active account, if present.</returns>
    public async Task<Account?> FindActive(AccountSubject subject, CancellationToken cancellationToken = default) => await Find(subject) is { Status: AccountStatus.Active } account ? account : null;
}

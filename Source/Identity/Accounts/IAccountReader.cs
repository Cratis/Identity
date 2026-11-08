// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts;

/// <summary>Reads only active accounts for session and grant admission.</summary>
internal interface IAccountReader
{
    /// <summary>Finds the active account by its immutable subject.</summary>
    /// <param name="subject">The account subject.</param>
    /// <param name="cancellationToken">The request cancellation.</param>
    /// <returns>The active account, if present.</returns>
    Task<Account?> FindActive(AccountSubject subject, CancellationToken cancellationToken = default);
}

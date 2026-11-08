// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>The account-stream seam of the erasure workflow.</summary>
internal interface IAccountErasureLog
{
    /// <summary>Reads the erasure decision state.</summary>
    /// <param name="account">The account subject.</param>
    /// <returns>The state.</returns>
    Task<ErasureState> Read(AccountSubject account);

    /// <summary>Appends one Internal fact to the account stream.</summary>
    /// <param name="account">The account subject.</param>
    /// <param name="fact">The fact.</param>
    /// <returns>The append completion.</returns>
    Task Append(AccountSubject account, object fact);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>
/// The account-stream seam of the erasure workflow. A real implementation MUST append with optimistic concurrency
/// guarded by the state it read, or as a one-time append keyed on the erasure, so concurrent coordinators cannot
/// both record completion or duplicate a request/failure.
/// <see cref="ChronicleAccountErasureLog"/> provides that guard with kernel-keyed one-time appends.
/// TODO: nothing registers the log, the stages, the coordinator or an enabling policy in production; erasure stays fail-closed until the boundary is approved.
/// </summary>
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

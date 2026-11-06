// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Legacy;

/// <summary>Preserves the Mongo account read path until the separate default switch.</summary>
/// <param name="accounts">The legacy account store.</param>
public class MongoAccountReader(IAccounts accounts) : IAccountReader
{
    /// <inheritdoc/>
    public Task<Account?> FindActive(AccountSubject subject, CancellationToken cancellationToken = default) => accounts.FindById(subject.Value, cancellationToken);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>Resolves the tenant or realm namespaces one erasure covers.</summary>
internal interface IErasureTenantScope
{
    /// <summary>Gets the namespaces whose subject keys must be destroyed.</summary>
    /// <param name="account">The account subject.</param>
    /// <returns>The namespaces; empty means the scope is not decided.</returns>
    Task<IReadOnlyList<string>> NamespacesFor(AccountSubject account);
}

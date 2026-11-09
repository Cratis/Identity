// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Erasing;

/// <summary>The fail-closed default: no namespace is in scope, so the workflow refuses to run.</summary>
internal class NoErasureTenantScope : IErasureTenantScope
{
    /// <inheritdoc/>
    // TODO(#51): decide whether one erasure covers every realm/namespace the person exists in or a single realm.
    public Task<IReadOnlyList<string>> NamespacesFor(AccountSubject account) => Task.FromResult<IReadOnlyList<string>>([]);
}

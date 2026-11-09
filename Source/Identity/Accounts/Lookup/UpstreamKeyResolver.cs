// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Lookup;

/// <summary>
/// Resolves an external key through the keyed lookup digest. The result is a hint: the account is read again and must be active.
/// </summary>
/// <param name="digester">The <see cref="UpstreamKeyDigester"/>.</param>
/// <param name="lookups">The <see cref="UpstreamLookups"/>.</param>
internal class UpstreamKeyResolver(UpstreamKeyDigester digester, UpstreamLookups lookups) : IUpstreamKeyResolver
{
    /// <inheritdoc/>
    public Task<UpstreamKeyMatch?> Find(UpstreamIssuer issuer, UpstreamSubject subject) => lookups.Find(digester.Digest(issuer, subject));
}

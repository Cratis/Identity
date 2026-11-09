// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Lookup;

/// <summary>
/// Defines the resolution of an external issuer and subject to the account that linked them.
/// </summary>
internal interface IUpstreamKeyResolver
{
    /// <summary>
    /// Finds the reservation of an external issuer and subject.
    /// </summary>
    /// <param name="issuer">The <see cref="UpstreamIssuer"/>.</param>
    /// <param name="subject">The <see cref="UpstreamSubject"/>.</param>
    /// <returns>The <see cref="UpstreamKeyMatch"/>, or <see langword="null"/> when no account ever linked the key.</returns>
    Task<UpstreamKeyMatch?> Find(UpstreamIssuer issuer, UpstreamSubject subject);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Authorization.TokenExchange;

/// <summary>
/// Represents the verified facts of a subject token. Nothing here is ever logged.
/// </summary>
/// <param name="Issuer">The configured issuer that signed the token.</param>
/// <param name="Subject">The subject at that issuer.</param>
/// <param name="AuthTime">The authentication time in Unix seconds, if the token carried one.</param>
/// <param name="Expiry">When the token expires.</param>
/// <param name="IssuedAt">When the token was issued.</param>
/// <param name="ReplayKey">The key identifying this token for single use.</param>
internal sealed record ValidatedSubject(UpstreamIssuer Issuer, UpstreamSubject Subject, long? AuthTime, DateTimeOffset Expiry, DateTimeOffset IssuedAt, string ReplayKey);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Accounts;

/// <summary>
/// The server-side record of a browser sign-in: which account signed in through which link.
/// </summary>
/// <param name="SessionId">The opaque session identifier carried by the session cookie.</param>
/// <param name="Subject">The account the session belongs to.</param>
/// <param name="LinkId">The account link the sign-in authenticated through.</param>
/// <param name="AuthTime">When the upstream authentication was verified.</param>
/// <param name="RevokedAt">When the session was revoked, or null while it stands.</param>
internal record BrowserSession(string SessionId, string Subject, Guid LinkId, DateTimeOffset AuthTime, DateTimeOffset? RevokedAt = null);

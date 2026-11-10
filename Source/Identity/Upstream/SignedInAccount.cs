// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;

namespace Cratis.Identity.Upstream;

/// <summary>
/// The account signed in to the Identity site and when its upstream authentication was verified.
/// </summary>
/// <param name="Account">The active <see cref="Account"/>.</param>
/// <param name="AuthTime">When the upstream authentication of the session was verified.</param>
/// <param name="SessionId">The opaque browser-session identifier, if the session has one.</param>
internal record SignedInAccount(Account Account, DateTimeOffset AuthTime, string? SessionId = null);

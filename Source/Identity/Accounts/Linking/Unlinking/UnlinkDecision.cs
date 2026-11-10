// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Linking.Unlinking;

/// <summary>
/// Guarded confirmed links and the trusted unlink time.
/// </summary>
/// <param name="Account">The authenticated owning account subject.</param>
/// <param name="State">The guarded account link state.</param>
/// <param name="At">The trusted unlink time.</param>
internal record UnlinkDecision(AccountSubject Account, AccountLinkState State, DateTimeOffset At);

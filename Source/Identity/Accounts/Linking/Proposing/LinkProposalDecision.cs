// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts.Linking.Proposing;

/// <summary>
/// Guarded account state and trusted observations for the proposal decision.
/// </summary>
/// <param name="Account">The authenticated account subject.</param>
/// <param name="State">The guarded account link state.</param>
/// <param name="Authentication">The fresh session-bound upstream proof.</param>
/// <param name="Intent">The explicit ceremony intent and trusted connection policy.</param>
/// <param name="Reserved">Whether the upstream key already has a permanent reservation.</param>
/// <param name="Hold">The realm's minimum link hold duration.</param>
/// <param name="At">The trusted decision time.</param>
internal record LinkProposalDecision(AccountSubject Account, AccountLinkState State, VerifiedAuthentication Authentication, LinkIntent Intent, bool Reserved, TimeSpan Hold, DateTimeOffset At);

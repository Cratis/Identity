// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Linking.Rejecting;

/// <summary>
/// Guarded state for an owner rejection.
/// </summary>
/// <param name="Account">The authenticated owning account subject.</param>
/// <param name="State">The guarded account link state.</param>
internal record LinkRejectionDecision(AccountSubject Account, AccountLinkState State);

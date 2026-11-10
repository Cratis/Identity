// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

namespace Cratis.Identity.Accounts.Linking.Confirming;

/// <summary>The result inspected only after the command's guarded batch commits.</summary>
/// <param name="LinkId">The confirmed link, or null when the proposal was quarantined.</param>
internal record LinkConfirmationOutcome(LinkId? LinkId);

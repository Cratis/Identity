// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts.Linking.Confirming;

/// <summary>The guarded inputs for committing a confirmed or cleared proposal.</summary>
/// <param name="Account">The authenticated owner.</param>
/// <param name="State">The state read after enrolling the account guard.</param>
/// <param name="Authentication">The fresh owner-session-bound upstream proof.</param>
/// <param name="Digest">The exact upstream key digest.</param>
/// <param name="Reserved">Whether the permanent reservation is already held.</param>
/// <param name="Hold">The realm's minimum hold.</param>
/// <param name="At">The trusted decision time.</param>
internal record LinkConfirmationDecision(AccountSubject Account, AccountLinkState State, VerifiedAuthentication Authentication, UpstreamLookupDigest Digest, bool Reserved, TimeSpan Hold, DateTimeOffset At);

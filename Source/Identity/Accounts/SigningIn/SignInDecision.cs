// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts.SigningIn;

/// <summary>
/// The trusted observations and guarded, caught-up account state for one decision attempt.
/// </summary>
/// <param name="Subject">The account event source.</param>
/// <param name="Authentication">The verified authentication.</param>
/// <param name="Digest">The exact upstream key pseudonym.</param>
/// <param name="Profile">The observed profile.</param>
/// <param name="Reservation">The permanent key reservation.</param>
/// <param name="Account">The caught-up account.</param>
/// <param name="At">The decision time.</param>
internal record SignInDecision(AccountSubject Subject, VerifiedAuthentication Authentication, UpstreamLookupDigest Digest, ObservedProfile Profile, UpstreamKeyMatch? Reservation, Account? Account, DateTimeOffset At);

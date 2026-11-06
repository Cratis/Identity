// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Identity.Upstream;

namespace Cratis.Identity.Accounts.Legacy;

/// <summary>Preserves legacy account subjects until the separate default switch.</summary>
/// <param name="accounts">The legacy account store.</param>
public class MongoAccountSignIn(IAccounts accounts) : IAccountSignIn
{
    /// <inheritdoc/>
    public Task<Account> SignIn(VerifiedAuthentication authentication, ObservedProfile profile) => accounts.RecordGitHubSignIn(new(long.Parse(authentication.Subject.Value, CultureInfo.InvariantCulture), profile.Login.Value, profile.Name.Value));
}

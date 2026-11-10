// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Realm;
using Cratis.Identity.Sessions;

namespace Cratis.Identity.ConsentFlow;

/// <summary>The server-owned caller binding.</summary>
/// <param name="Account">The active account identifier.</param>
/// <param name="Session">The authenticated session identifier.</param>
/// <param name="Realm">The configured issuer realm.</param>
internal record ConsentCallerIdentity(AccountSubject Account, SessionId Session, RealmId Realm);

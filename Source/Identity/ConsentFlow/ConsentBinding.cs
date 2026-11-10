// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Clients;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Realm;
using Cratis.Identity.Resources;
using Cratis.Identity.Sessions;
using Cratis.Identity.Tenants;

namespace Cratis.Identity.ConsentFlow;

/// <summary>Immutable, server-validated authorization parameters; never take these from a consent POST.</summary>
/// <param name="Realm">The issuer realm.</param>
/// <param name="Account">The authenticated account.</param>
/// <param name="Client">The registered client.</param>
/// <param name="Tenant">The selected tenant.</param>
/// <param name="Resource">The validated resource.</param>
/// <param name="Scopes">The canonical, sorted scope set.</param>
/// <param name="Session">The authenticated browser session identity.</param>
/// <param name="AuthorizationRequest">The distinct original authorization request identity, recovered from server-side or protected state, never untrusted form data.</param>
internal record ConsentBinding(RealmId Realm, AccountSubject Account, ClientId Client, TenantId Tenant, string Resource, ScopeSet Scopes, SessionId Session, AuthorizationRequestId AuthorizationRequest);

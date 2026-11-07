// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Contracts.Accounts;

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
public record ConsentBinding(string Realm, AccountSubject Account, string Client, string Tenant, string Resource, string Scopes, string Session, AuthorizationRequestId AuthorizationRequest);

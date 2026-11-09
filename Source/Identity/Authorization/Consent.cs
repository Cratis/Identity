// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Tenants;

namespace Cratis.Identity.Authorization;

/// <summary>
/// Represents what the consent page asks the user to approve.
/// </summary>
/// <param name="ClientId">The client identifier.</param>
/// <param name="ClientName">The registered display name of the client.</param>
/// <param name="RedirectUri">The redirect URI the authorization code will be sent to.</param>
/// <param name="Grant">The <see cref="Authorization.Grant"/> being requested.</param>
/// <param name="Tenants">The tenants the user can select.</param>
/// <param name="Account">The signed-in <see cref="Accounts.Account"/>.</param>
/// <param name="Parameters">The OAuth parameters of the authorization request, carried through the form.</param>
/// <param name="Error">An error to show, for example when no tenant was selected.</param>
internal record Consent(
    string ClientId,
    string ClientName,
    Uri RedirectUri,
    Grant Grant,
    IEnumerable<SelectableTenant> Tenants,
    Account Account,
    IEnumerable<KeyValuePair<string, string>> Parameters,
    string? Error = null);

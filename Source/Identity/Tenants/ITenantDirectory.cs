// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Resources;

namespace Cratis.Identity.Tenants;

/// <summary>
/// Defines the source of the tenants an account can select for a resource server.
/// </summary>
/// <remarks>
/// The product that owns the resource server owns tenancy and membership. Identity only records which tenant
/// the user chose at authorization time and re-checks it on every refresh; resource servers must still enforce
/// membership themselves. An implementation that calls a product endpoint (for example the Direct
/// endpoint <c language="text">/api/internal/tenants/selectable</c>) plugs in here.
/// </remarks>
public interface ITenantDirectory
{
    /// <summary>
    /// Gets the tenants an account can select for a resource server.
    /// </summary>
    /// <param name="account">The signed-in <see cref="Account"/>.</param>
    /// <param name="resource">The <see cref="ResourceServer"/> being authorized.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The selectable tenants; empty when the account may not use the resource server at all.</returns>
    Task<IEnumerable<SelectableTenant>> SelectableTenants(Account account, ResourceServer resource, CancellationToken cancellationToken = default);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Configuration;
using Cratis.Identity.Resources;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Tenants;

/// <summary>
/// Routes tenant lookups to the product callback, allowing configured fixtures only in Development.
/// </summary>
/// <param name="options">The current resource configuration.</param>
/// <param name="environment">The host environment.</param>
/// <param name="callback">The product callback.</param>
/// <param name="configured">The Development fixture directory.</param>
internal class TenantDirectory(
    IOptionsMonitor<IdentityOptions> options,
    IHostEnvironment environment,
    ProductTenantsCallback callback,
    ConfiguredTenantDirectory configured) : ITenantDirectory
{
    /// <inheritdoc/>
    public Task<TenantLookup> SelectableTenants(Account account, ResourceServer resource, CancellationToken cancellationToken = default)
    {
        var configuration = options.CurrentValue.Resources.FirstOrDefault(_ => string.Equals(_.Audience, resource.Audience, StringComparison.Ordinal));
        if (configuration?.TenantsCallback is { } settings)
        {
            return callback.SelectableTenants(account, resource, settings, cancellationToken);
        }

        return environment.IsDevelopment()
            ? configured.SelectableTenants(account, resource, cancellationToken)
            : Task.FromResult<TenantLookup>(new TenantsUnavailable(TenantsUnavailableReason.NotConfigured));
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Configuration;
using Cratis.Identity.Resources;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Tenants;

/// <summary>
/// Represents an implementation of <see cref="ITenantDirectory"/> backed by the tenants configured per resource
/// server. Development fixtures only. It fails closed: an account that is not a listed member of a tenant cannot select it unless the
/// tenant explicitly allows all accounts.
/// </summary>
/// <param name="options">The <see cref="IdentityOptions"/>.</param>
public class ConfiguredTenantDirectory(IOptionsMonitor<IdentityOptions> options) : ITenantDirectory
{
    /// <inheritdoc/>
    public Task<TenantLookup> SelectableTenants(Account account, ResourceServer resource, CancellationToken cancellationToken = default)
    {
        var gitHubId = account.GitHubId.Value;
        var tenants = options.CurrentValue.Resources
            .Where(_ => string.Equals(_.Audience, resource.Audience, StringComparison.Ordinal))
            .SelectMany(_ => _.Tenants)
            .Where(_ => _.AllowAllAccounts || _.Members.Contains(gitHubId, StringComparer.Ordinal))
            .Select(_ => new SelectableTenant(_.Id, string.IsNullOrWhiteSpace(_.Name) ? _.Id : _.Name))
            .ToArray();

        return Task.FromResult<TenantLookup>(new TenantsListed(tenants));
    }
}

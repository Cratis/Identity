// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization;
using Cratis.Identity.Resources;
using Cratis.Identity.Tenants;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.ConsentFlow;

/// <summary>Revalidates current account, client, resource, scope and tenant catalogue inputs.</summary>
/// <param name="accounts">The canonical active account reader.</param>
/// <param name="grants">The existing resource and protocol-scope policy.</param>
/// <param name="tenants">The tenant directory.</param>
/// <param name="applications">The current client catalogue.</param>
/// <param name="resources">The resource audience catalogue.</param>
internal sealed class ConsentEligibilityReader(IAccountReader accounts, IGrantPolicy grants, ITenantDirectory tenants, IOpenIddictApplicationManager applications, IResourceCatalog resources) : IConsentEligibility
{
    /// <inheritdoc/>
    public async Task<ConsentEligibility?> Evaluate(ConsentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await Read(request, cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    async Task<ConsentEligibility?> Read(ConsentRequest request, CancellationToken cancellationToken)
    {
        if (await accounts.FindActive(request.Binding.Account, cancellationToken) is not { Status: AccountStatus.Active } account) return null;
        var resource = resources.FindByAudience(request.Binding.Resource);
        if (resource is null) return null;
        var scopes = request.Binding.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var grant = ExactGrant(request, resource, scopes);
        if (grant is null) return null;
        var application = await applications.FindByClientIdAsync(request.Binding.Client, cancellationToken);
        if (application is null || !await CanRequest(application, scopes, cancellationToken)) return null;
        if (await tenants.SelectableTenants(account, grant.Resource, cancellationToken) is not TenantsListed listed) return null;
        var selectable = listed.Tenants.ToArray();
        return selectable.Length == 0 ? null : new(grant, selectable, await applications.GetDisplayNameAsync(application, cancellationToken));
    }

    Grant? ExactGrant(ConsentRequest request, ResourceServer resource, string[] scopes)
    {
        if (grants.Evaluate(new(scopes, resource.Indicators)) is not Grantable grant) return null;
        return grant.Grant.Resource.Audience == request.Binding.Resource && scopes.ToHashSet(StringComparer.Ordinal).SetEquals(grant.Grant.Scopes) ? grant.Grant : null;
    }

    async Task<bool> CanRequest(object application, string[] scopes, CancellationToken cancellationToken)
    {
        var permissions = await applications.GetPermissionsAsync(application, cancellationToken);
        var required = scopes.Where(_ => _ is not Scopes.OpenId and not Scopes.OfflineAccess).Select(_ => Permissions.Prefixes.Scope + _)
            .Append(Permissions.Endpoints.Authorization).Append(Permissions.GrantTypes.AuthorizationCode);
        return required.All(permissions.Contains) && (!scopes.Contains(Scopes.OfflineAccess, StringComparer.Ordinal) || permissions.Contains(Permissions.GrantTypes.RefreshToken));
    }
}

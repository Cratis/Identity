// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.given;
using Cratis.Identity.Resources;
using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_ConfiguredTenantDirectory;

public class when_listing_selectable_tenants : Specification
{
    ConfiguredTenantDirectory _directory;
    ResourceServer _direct;
    IEnumerable<SelectableTenant> _forMember;
    IEnumerable<SelectableTenant> _forStranger;
    IEnumerable<SelectableTenant> _forUnlinked;

    void Establish()
    {
        var options = Configurations.TwoResourceServers();
        _directory = new ConfiguredTenantDirectory(options.AsMonitor());
        _direct = new ResourceCatalog(options.AsOptions()).FindByAudience(Configurations.DirectAudience);
    }

    async Task Because()
    {
        _forMember = ((TenantsListed)await _directory.SelectableTenants(GitHubAccounts.Create(new("7d9d5c9e-0000-4000-8000-000000000001"), "Einar", null, Configurations.MemberGitHubId, "einari"), _direct)).Tenants;
        _forStranger = ((TenantsListed)await _directory.SelectableTenants(GitHubAccounts.Create(new("7d9d5c9e-0000-4000-8000-000000000002"), "Stranger", null, "42", "stranger"), _direct)).Tenants;
        _forUnlinked = ((TenantsListed)await _directory.SelectableTenants(new Account(new("7d9d5c9e-0000-4000-8000-000000000003"), "Unlinked", null, AccountStatus.Active, []), _direct)).Tenants;
    }

    [Fact] void should_offer_an_account_without_a_github_link_only_open_tenants() => _forUnlinked.Select(_ => _.Id).ShouldContainOnly("public");
    [Fact] void should_offer_a_member_the_tenants_it_is_listed_in_and_open_tenants() => _forMember.Select(_ => _.Id).ShouldContainOnly("cratis", "public");
    [Fact] void should_offer_a_stranger_only_open_tenants() => _forStranger.Select(_ => _.Id).ShouldContainOnly("public");
    [Fact] void should_not_offer_tenants_of_another_resource() => _forMember.Select(_ => _.Id).ShouldNotContain("studio-tenant");
}

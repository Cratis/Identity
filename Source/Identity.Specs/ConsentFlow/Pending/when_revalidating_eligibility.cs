// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization;
using Cratis.Identity.Resources;
using Cratis.Identity.Tenants;
using OpenIddict.Abstractions;

namespace Cratis.Identity.ConsentFlow.Pending;

public class when_revalidating_eligibility : Deciding.given.an_activation
{
    readonly List<ConsentEligibility?> _refused = [];
    ConsentEligibility? _valid;

    async Task Because()
    {
        var accounts = Substitute.For<IAccountReader>();
        var grants = Substitute.For<IGrantPolicy>();
        var tenants = Substitute.For<ITenantDirectory>();
        var applications = Substitute.For<IOpenIddictApplicationManager>();
        var application = new object();
        var active = new Account(binding.Account, "Spec Account", null, "1234", "spec-account", AccountStatus.Active, []);
        var resource = new ResourceServer(binding.Resource, "resource", [binding.Resource], [new("read", "Read")]);
        accounts.FindActive(binding.Account, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Account?>(active));
        grants.Evaluate(Arg.Any<GrantRequest>()).Returns(new Grantable(new(resource, ["openid", "read"])));
        tenants.SelectableTenants(Arg.Any<Account>(), resource, Arg.Any<CancellationToken>()).Returns(Task.FromResult<TenantLookup>(new TenantsListed([new(binding.Tenant, "Tenant")])));
        applications.FindByClientIdAsync(binding.Client, Arg.Any<CancellationToken>()).Returns(new ValueTask<object?>(application));
        applications.GetPermissionsAsync(application, Arg.Any<CancellationToken>()).Returns(new ValueTask<ImmutableArray<string>>(["ept:authorization", "gt:authorization_code", "scp:read"]));
        applications.GetDisplayNameAsync(application, Arg.Any<CancellationToken>()).Returns(new ValueTask<string?>("Client <script>name</script>"));
        var resources = Substitute.For<IResourceCatalog>();
        resources.FindByAudience(binding.Resource).Returns(resource);
        var reader = new ConsentEligibilityReader(accounts, grants, tenants, applications, resources);
        _valid = await reader.Evaluate(request, CancellationToken.None);
        accounts.FindActive(binding.Account, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Account?>(active with { Status = AccountStatus.Disabled }));
        _refused.Add(await reader.Evaluate(request, CancellationToken.None));
        accounts.FindActive(binding.Account, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Account?>(active with { IsTombstoned = true }));
        _refused.Add(await reader.Evaluate(request, CancellationToken.None));
        accounts.FindActive(binding.Account, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Account?>(null));
        _refused.Add(await reader.Evaluate(request, CancellationToken.None));
        accounts.FindActive(binding.Account, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Account?>(active));
        applications.FindByClientIdAsync(binding.Client, Arg.Any<CancellationToken>()).Returns(new ValueTask<object?>((object?)null));
        _refused.Add(await reader.Evaluate(request, CancellationToken.None));
        applications.FindByClientIdAsync(binding.Client, Arg.Any<CancellationToken>()).Returns(new ValueTask<object?>(application));
        applications.GetPermissionsAsync(application, Arg.Any<CancellationToken>()).Returns(new ValueTask<ImmutableArray<string>>([]));
        _refused.Add(await reader.Evaluate(request, CancellationToken.None));
        applications.GetPermissionsAsync(application, Arg.Any<CancellationToken>()).Returns(new ValueTask<ImmutableArray<string>>(["ept:authorization", "gt:authorization_code", "scp:read"]));
        grants.Evaluate(Arg.Any<GrantRequest>()).Returns(new Grantable(new(resource, ["openid"])));
        _refused.Add(await reader.Evaluate(request, CancellationToken.None));
        grants.Evaluate(Arg.Any<GrantRequest>()).Returns(new NotGrantable("invalid_scope", "refused"));
        _refused.Add(await reader.Evaluate(request, CancellationToken.None));
        grants.Evaluate(Arg.Any<GrantRequest>()).Returns(new Grantable(new(resource, ["openid", "read"])));
        tenants.SelectableTenants(Arg.Any<Account>(), resource, Arg.Any<CancellationToken>()).Returns(Task.FromResult<TenantLookup>(new TenantsUnavailable(TenantsUnavailableReason.Unreachable)));
        _refused.Add(await reader.Evaluate(request, CancellationToken.None));
        tenants.SelectableTenants(Arg.Any<Account>(), resource, Arg.Any<CancellationToken>()).Returns(Task.FromResult<TenantLookup>(new TenantsListed([])));
        _refused.Add(await reader.Evaluate(request, CancellationToken.None));
    }

    [Fact] void should_allow_current_eligible_catalogue_inputs() => _valid.ShouldNotBeNull();
    [Fact] void should_resolve_the_display_name_from_the_client_catalogue() => _valid.ClientName.ShouldEqual("Client <script>name</script>");
    [Fact] void should_revalidate_every_denial_case() => _refused.Count.ShouldEqual(9);
    [Fact] void should_refuse_ineligible_or_unavailable_inputs() => _refused.TrueForAll(_ => _ is null).ShouldBeTrue();
}

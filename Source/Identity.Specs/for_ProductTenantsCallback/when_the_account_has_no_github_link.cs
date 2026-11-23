// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_ProductTenantsCallback;

[PerFactSpecificationLifecycle]
public class when_the_account_has_no_github_link : given.a_tenants_callback
{
    [Fact]
    async Task should_not_call_the_product_for_a_non_github_only_account()
    {
        account = account with
        {
            LinkedUpstreamKeys = [new LinkedUpstreamKey(new(Guid.NewGuid()), new(Guid.NewGuid()), new("https://issuer.example"), new("external-subject"), new("external-login"), DateTimeOffset.UnixEpoch)]
        };
        await Lookup(TestContext.Current.CancellationToken);
        handler.Requests.ShouldBeEmpty();
        ((TenantsListed)result).Tenants.ShouldBeEmpty();
    }

    [Fact]
    async Task should_not_call_the_product_for_an_account_without_links()
    {
        account = account with { LinkedUpstreamKeys = [] };
        await Lookup(TestContext.Current.CancellationToken);
        handler.Requests.ShouldBeEmpty();
        ((TenantsListed)result).Tenants.ShouldBeEmpty();
    }
}

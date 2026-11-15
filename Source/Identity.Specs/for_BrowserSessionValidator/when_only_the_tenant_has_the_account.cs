// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts.Accounts;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.for_BrowserSessionValidator;

public class when_only_the_tenant_has_the_account : given.a_browser_session
{
    void Establish()
    {
        _ = _request.ServiceProvider.GetRequiredService<IAccountReader>();
        _tenantAccounts.FindActive(Arg.Any<AccountSubject>(), Arg.Any<CancellationToken>()).Returns(_account);
        _accounts.FindActive(Arg.Any<AccountSubject>(), Arg.Any<CancellationToken>()).Returns((Account?)null);
        Recorded();
    }

    async Task Because() => _result = await _validator.IsValid(_principal, CancellationToken.None);

    [Fact] void should_reject_the_session() => _result.ShouldBeFalse();
    [Fact] void should_look_for_the_account_only_in_the_realm() => _accountNamespace.ShouldEqual(EventStoreNamespaceName.Default);
    [Fact] void should_leave_host_work_in_the_tenant() => _namespaces.Resolve().ShouldEqual(new EventStoreNamespaceName("tenant-a"));
}

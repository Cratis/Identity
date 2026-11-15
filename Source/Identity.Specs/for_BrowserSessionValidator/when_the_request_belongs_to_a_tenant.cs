// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Identity.Accounts;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.for_BrowserSessionValidator;

public class when_the_request_belongs_to_a_tenant : given.a_browser_session
{
    void Establish()
    {
        _ = _request.ServiceProvider.GetRequiredService<IAccountReader>();
        Recorded();
    }

    async Task Because() => _result = await _validator.IsValid(_principal, CancellationToken.None);

    [Fact] void should_accept_the_realm_session() => _result.ShouldBeTrue();
    [Fact] void should_resolve_the_account_reader_in_the_realm() => _accountNamespace.ShouldEqual(EventStoreNamespaceName.Default);
    [Fact] void should_leave_host_work_in_the_tenant() => _namespaces.Resolve().ShouldEqual(new EventStoreNamespaceName("tenant-a"));
}

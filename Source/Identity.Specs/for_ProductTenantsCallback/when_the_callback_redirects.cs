// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_ProductTenantsCallback;

public class when_the_callback_redirects : given.a_tenants_callback
{
    void Establish() => handler.Status = HttpStatusCode.Found;
    Task Because() => Lookup();

    [Fact] void should_fail_closed() => result.ShouldEqual(new TenantsUnavailable(TenantsUnavailableReason.Unreachable));
    [Fact] void should_make_only_one_request() => handler.Requests.Count.ShouldEqual(1);
}

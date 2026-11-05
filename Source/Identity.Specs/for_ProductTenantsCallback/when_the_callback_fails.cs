// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_ProductTenantsCallback;

public class when_the_callback_fails : given.a_tenants_callback
{
    void Establish() => handler.Status = HttpStatusCode.ServiceUnavailable;
    Task Because() => Lookup();

    [Fact] void should_be_unavailable() => result.ShouldEqual(new TenantsUnavailable(TenantsUnavailableReason.Unreachable));
}

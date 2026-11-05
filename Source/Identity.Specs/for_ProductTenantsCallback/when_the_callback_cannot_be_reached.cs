// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_ProductTenantsCallback;

public class when_the_callback_cannot_be_reached : given.a_tenants_callback
{
    void Establish() => handler.BeforeResponse = _ => throw new HttpRequestException();
    Task Because() => Lookup();

    [Fact] void should_be_unreachable() => result.ShouldEqual(new TenantsUnavailable(TenantsUnavailableReason.Unreachable));
}

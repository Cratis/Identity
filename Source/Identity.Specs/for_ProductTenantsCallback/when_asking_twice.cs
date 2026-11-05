// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_ProductTenantsCallback;

public class when_asking_twice : given.a_tenants_callback
{
    async Task Because()
    {
        await Lookup();
        await Lookup();
    }

    [Fact] void should_not_cache_the_answer() => handler.Requests.Count.ShouldEqual(2);
}

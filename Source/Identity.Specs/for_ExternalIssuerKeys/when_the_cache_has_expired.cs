// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_ExternalIssuerKeys;

public class when_the_cache_has_expired : given.an_issuer_that_serves_keys
{
    async Task Because()
    {
        await Get();
        _time.Now = _time.Now.AddMinutes(11);
        await Get();
    }

    [Fact] void should_fetch_again() => _requests.ShouldEqual(2);
}

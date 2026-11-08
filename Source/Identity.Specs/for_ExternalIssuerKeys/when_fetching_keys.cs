// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.given;

namespace Cratis.Identity.for_ExternalIssuerKeys;

public class when_fetching_keys : given.an_issuer_that_serves_keys
{
    async Task Because()
    {
        await Get();
        await Get(ExternalIssuerFixture.RsaKeyId);
        _time.Now = _time.Now.AddMinutes(5);
        await Get();
    }

    [Fact] void should_return_the_published_keys() => _result.Count.ShouldEqual(2);
    [Fact] void should_fetch_only_once_within_the_cache_lifetime() => _requests.ShouldEqual(1);
}

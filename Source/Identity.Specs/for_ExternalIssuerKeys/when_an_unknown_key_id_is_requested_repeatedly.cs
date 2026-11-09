// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.for_ExternalIssuerKeys;

public class when_an_unknown_key_id_is_requested_repeatedly : given.an_issuer_that_serves_keys
{
    async Task Because()
    {
        await Get();
        await Get("unknown-1");
        await Get("unknown-2");
        _time.Now = _time.Now.AddSeconds(61);
        await Get("unknown-3");
    }

    [Fact] void should_refresh_at_most_once_per_interval() => _requests.ShouldEqual(2);
}

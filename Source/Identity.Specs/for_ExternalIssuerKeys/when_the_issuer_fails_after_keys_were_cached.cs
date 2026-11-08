// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Identity.for_ExternalIssuerKeys;

public class when_the_issuer_fails_after_keys_were_cached : given.an_issuer_that_serves_keys
{
    async Task Because()
    {
        await Get();
        _respond = () => new(HttpStatusCode.InternalServerError);
        _time.Now = _time.Now.AddMinutes(30);
        await Get();
    }

    [Fact] void should_keep_serving_the_stale_keys() => _result!.Count.ShouldEqual(2);
}

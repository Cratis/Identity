// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Identity.for_ExternalIssuerKeys;

public class when_the_issuer_fails_for_longer_than_the_stale_lifetime : given.an_issuer_that_serves_keys
{
    async Task Because()
    {
        await Get();
        _respond = () => new(HttpStatusCode.InternalServerError);
        _time.Now = _time.Now.AddHours(2);
        await Get();
    }

    [Fact] void should_return_no_keys() => _result.ShouldBeNull();
}

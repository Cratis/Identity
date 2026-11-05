// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;
using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_ProductTenantsCallback;

public class when_the_callback_stalls_while_streaming_the_body : given.a_tenants_callback
{
    void Establish()
    {
        // This body deliberately never finishes, so the same deadline must cover reading it.
        handler.Content = () => new StreamContent(new given.CallbackBodyStream { Stall = true }) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } };
    }
    Task Because() => Lookup();

    [Fact] void should_time_out() => result.ShouldEqual(new TenantsUnavailable(TenantsUnavailableReason.TimedOut));
}

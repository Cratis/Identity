// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;
using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_ProductTenantsCallback;

public class when_the_callback_body_connection_breaks : given.a_tenants_callback
{
    void Establish() => handler.Content = () => new StreamContent(new given.CallbackBodyStream { Broken = true }) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } };
    Task Because() => Lookup();

    [Fact] void should_be_unreachable() => result.ShouldEqual(new TenantsUnavailable(TenantsUnavailableReason.Unreachable));
}

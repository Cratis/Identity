// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;
using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_ProductTenantsCallback;

public class when_the_callback_streams_an_oversized_body_without_a_length : given.a_tenants_callback
{
    given.CallbackBodyStream _stream;

    void Establish()
    {
        _stream = new given.CallbackBodyStream();
        handler.Content = () => new StreamContent(_stream) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } };
    }

    Task Because() => Lookup();

    [Fact] void should_be_malformed() => result.ShouldEqual(new TenantsUnavailable(TenantsUnavailableReason.Malformed));
    [Fact] void should_stop_at_the_limit() => _stream.BytesRead.ShouldEqual((64 * 1024) + 1);
}

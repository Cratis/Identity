// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_ProductTenantsCallback;

public class when_the_callback_answers_with_malformed_json : given.a_tenants_callback
{
    [Theory]
    [InlineData("{")]
    [InlineData(/*lang=json*/ "[]")]
    [InlineData(/*lang=json*/ "{\"subject\":\"\\uD800\",\"tenants\":[]}")]
    [InlineData(/*lang=json*/ "{\"subject\":\"7d9d5c9e-0000-4000-8000-000000000001\",\"tenants\":[{\"id\":\"\\uD800\"}]}")]
    [InlineData(/*lang=json*/ "{\"subject\":\"7d9d5c9e-0000-4000-8000-000000000001\",\"tenants\":[{\"id\":\"one\",\"name\":\"\\uD800\"}]}")]
    [InlineData(/*lang=json*/ "{\"subject\":\"7d9d5c9e-0000-4000-8000-000000000001\"}")]
    [InlineData(/*lang=json*/ "{\"subject\":\"7d9d5c9e-0000-4000-8000-000000000001\",\"tenants\":{}}")]
    [InlineData(/*lang=json*/ "{\"subject\":\"7d9d5c9e-0000-4000-8000-000000000001\",\"tenants\":[null]}")]
    [InlineData(/*lang=json*/ "{\"subject\":\"7d9d5c9e-0000-4000-8000-000000000001\",\"tenants\":[],\"extra\":[[[[[[[[]]]]]]]]}")]
    async Task should_reject_the_body(string body)
    {
        handler.Body = body;
        await Lookup(TestContext.Current.CancellationToken);
        result.ShouldEqual(new TenantsUnavailable(TenantsUnavailableReason.Malformed));
    }

    [Fact]
    async Task should_reject_more_than_256_tenants()
    {
        handler.Body = $"{{\"subject\":\"{account.Id.Value}\",\"tenants\":[" + string.Join(',', Enumerable.Range(0, 257).Select(i => $"{{\"id\":\"t-{i}\"}}")) + "]}";
        await Lookup(TestContext.Current.CancellationToken);
        result.ShouldEqual(new TenantsUnavailable(TenantsUnavailableReason.Malformed));
    }
}

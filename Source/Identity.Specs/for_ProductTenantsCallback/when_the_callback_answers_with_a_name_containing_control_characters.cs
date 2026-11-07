// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_ProductTenantsCallback;

public class when_the_callback_answers_with_a_name_containing_control_characters : given.a_tenants_callback
{
    [Theory]
    [InlineData("\"bad\\nname\"")]
    [InlineData("\"\\t\"")]
    [InlineData("42")]
    [InlineData("null")]
    async Task should_refuse_the_name(string name)
    {
        handler.Body = $"{{\"subject\":\"{account.Id.Value}\",\"tenants\":[{{\"id\":\"one\",\"name\":{name}}}]}}";
        await Lookup(TestContext.Current.CancellationToken);
        result.ShouldEqual(new TenantsUnavailable(TenantsUnavailableReason.Malformed));
    }

    [Fact]
    async Task should_refuse_a_name_longer_than_128_characters()
    {
        handler.Body = $"{{\"subject\":\"{account.Id.Value}\",\"tenants\":[{{\"id\":\"one\",\"name\":\"{new string('a', 129)}\"}}]}}";
        await Lookup(TestContext.Current.CancellationToken);
        result.ShouldEqual(new TenantsUnavailable(TenantsUnavailableReason.Malformed));
    }
}

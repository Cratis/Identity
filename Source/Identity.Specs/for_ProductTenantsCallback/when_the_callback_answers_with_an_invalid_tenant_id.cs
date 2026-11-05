// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_ProductTenantsCallback;

public class when_the_callback_answers_with_an_invalid_tenant_id : given.a_tenants_callback
{
    [Theory]
    [InlineData("UPPER")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("")]
    [InlineData("-start")]
    [InlineData("end-")]
    [InlineData("one\n")]
    [InlineData("one two")]
    async Task should_reject_the_id(string id)
    {
        handler.Body = JsonSerializer.Serialize(new { subject = account.Id, tenants = new[] { new { id } } });
        await Lookup(TestContext.Current.CancellationToken);
        result.ShouldEqual(new TenantsUnavailable(TenantsUnavailableReason.Malformed));
    }
}

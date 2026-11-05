// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Identity.Tenants;

namespace Cratis.Identity.for_ProductTenantsCallback;

public class when_the_callback_refuses_the_credential : given.a_tenants_callback
{
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    async Task should_be_refused(HttpStatusCode status)
    {
        handler.Status = status;
        await Lookup(TestContext.Current.CancellationToken);
        result.ShouldEqual(new TenantsUnavailable(TenantsUnavailableReason.Refused));
    }
}

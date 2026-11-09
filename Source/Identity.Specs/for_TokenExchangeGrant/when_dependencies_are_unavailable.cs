// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.Tenants;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_TokenExchangeGrant;

public class when_dependencies_are_unavailable : given.a_token_exchange_request
{
    [Fact]
    async Task should_report_temporarily_unavailable_when_issuer_keys_are_unavailable()
    {
        _validator.Validate(default!, default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(Task.FromResult(SubjectTokenResult.Refused(SubjectTokenRefusal.KeysUnavailable)));
        await Handle();
        Error.ShouldEqual(Errors.TemporarilyUnavailable);
    }

    [Fact]
    async Task should_report_temporarily_unavailable_when_the_tenants_callback_fails()
    {
        _tenants.SelectableTenants(default!, default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(Task.FromResult<TenantLookup>(new TenantsUnavailable(TenantsUnavailableReason.Unreachable)));
        await Handle();
        Error.ShouldEqual(Errors.TemporarilyUnavailable);
    }
}

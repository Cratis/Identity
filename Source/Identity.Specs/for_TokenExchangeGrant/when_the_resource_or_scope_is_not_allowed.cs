// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_TokenExchangeGrant;

public class when_the_resource_or_scope_is_not_allowed : given.a_token_exchange_request
{
    [Fact]
    async Task should_refuse_an_unknown_resource()
    {
        var result = await Run(_ => _.SetParameter(Parameters.Resource, "https://unknown.example/mcp"));
        ErrorOf(result).ShouldEqual(Errors.InvalidTarget);
    }

    [Fact]
    async Task should_refuse_an_unowned_scope()
    {
        var result = await Run(_ => _.Scope = "nobody:owns-this");
        ErrorOf(result).ShouldEqual(Errors.InvalidScope);
    }

    [Fact]
    async Task should_refuse_offline_access()
    {
        var result = await Run(_ => _.Scope = $"direct:read {Scopes.OfflineAccess}");
        ErrorOf(result).ShouldEqual(Errors.InvalidScope);
    }

    [Fact]
    async Task should_refuse_a_client_that_is_not_enabled()
    {
        var result = await Run(_ => _.ClientId = "not-an-exchange-client");
        ErrorOf(result).ShouldEqual(Errors.UnauthorizedClient);
    }
}

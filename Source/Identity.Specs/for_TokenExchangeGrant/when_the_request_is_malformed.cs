// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_TokenExchangeGrant;

public class when_the_request_is_malformed : given.a_token_exchange_request
{
    [Fact]
    async Task should_refuse_an_actor_token()
    {
        var result = await Run(_ => _.SetParameter(TokenExchangeConstants.ActorToken, "actor"));
        ErrorOf(result).ShouldEqual(Errors.InvalidRequest);
    }

    [Fact]
    async Task should_refuse_a_missing_subject_token()
    {
        var result = await Run(_ => _.SetParameter(TokenExchangeConstants.SubjectToken, null));
        ErrorOf(result).ShouldEqual(Errors.InvalidRequest);
    }

    [Fact]
    async Task should_refuse_an_unsupported_subject_token_type()
    {
        var result = await Run(_ => _.SetParameter(TokenExchangeConstants.SubjectTokenType, "urn:other"));
        ErrorOf(result).ShouldEqual(Errors.InvalidRequest);
    }

    [Fact]
    async Task should_refuse_a_missing_tenant()
    {
        var result = await Run(_ => _.SetParameter(TokenExchangeConstants.Tenant, null));
        ErrorOf(result).ShouldEqual(Errors.InvalidRequest);
    }

    [Fact]
    async Task should_refuse_a_repeated_tenant()
    {
        var result = await Run(_ => _.SetParameter(TokenExchangeConstants.Tenant, new OpenIddictParameter(["cratis", "other"])));
        ErrorOf(result).ShouldEqual(Errors.InvalidRequest);
    }

    [Fact]
    async Task should_refuse_a_missing_scope()
    {
        var result = await Run(_ => _.Scope = null);
        ErrorOf(result).ShouldEqual(Errors.InvalidRequest);
    }

    [Fact]
    async Task should_refuse_an_unknown_requested_token_type()
    {
        var result = await Run(_ => _.SetParameter(TokenExchangeConstants.RequestedTokenType, "urn:other"));
        ErrorOf(result).ShouldEqual(Errors.InvalidRequest);
    }
}

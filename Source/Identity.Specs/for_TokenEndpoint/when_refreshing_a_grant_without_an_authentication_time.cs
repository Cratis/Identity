// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_TokenEndpoint;

public class when_refreshing_a_grant_without_an_authentication_time : RevocationSpecs.given.a_contended_refresh
{
    IResult _result;

    async Task Because() => _result = await ReachTokenEndpoint(null, new Dictionary<string, bool> { ["audience"] = true });

    [Fact] void should_refuse_the_grant() => _result.ShouldBeOfExactType<ForbidHttpResult>();
    [Fact] void should_answer_invalid_grant() => ((ForbidHttpResult)_result).Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.Error].ShouldEqual(Errors.InvalidGrant);
}

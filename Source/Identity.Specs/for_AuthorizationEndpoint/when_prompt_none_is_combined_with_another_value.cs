// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;

namespace Cratis.Identity.for_AuthorizationEndpoint;

public class when_prompt_none_is_combined_with_another_value : Specification
{
    IResult _result = null!;

    async Task Because()
    {
        var endpoint = new AuthorizationEndpoint(null!, null!, null!, null!, null!, null!);
        var context = new DefaultHttpContext();
        context.Features.Set(new OpenIddictServerAspNetCoreFeature
        {
            Transaction = new OpenIddictServerTransaction
            {
                Request = new OpenIddictRequest
                {
                    ClientId = "oidc-web",
                    Scope = "openid",
                    Nonce = "request-nonce",
                    Prompt = "login none"
                }
            }
        });
        _result = await endpoint.Handle(context);
    }

    [Fact] void should_refuse_the_request() => (_result is ForbidHttpResult).ShouldBeTrue();
    [Fact] void should_report_invalid_request() => ((ForbidHttpResult)_result).Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.Error].ShouldEqual("invalid_request");
}

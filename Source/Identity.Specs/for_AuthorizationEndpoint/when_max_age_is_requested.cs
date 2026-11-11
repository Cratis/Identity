// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Authorization.given;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;

namespace Cratis.Identity.for_AuthorizationEndpoint;

public class when_max_age_is_requested : Specification
{
    List<IResult> _results = [];

    async Task Because()
    {
        var endpoint = new AuthorizationEndpoint(null!, null!, null!, null!, null!, null!);
        foreach (var context in MaxAgeRequests.All())
        {
            context.Features.Set(new OpenIddictServerAspNetCoreFeature
            {
                Transaction = new OpenIddictServerTransaction
                {
                    Request = new OpenIddictRequest
                    {
                        ClientId = "oidc-web", Scope = "openid", Nonce = "request-nonce"
                    }
                }
            });
            _results.Add(await endpoint.Handle(context));
        }
    }

    [Fact] void should_refuse_without_looking_up_a_session_or_redirecting_to_login() => _results.All(result => result is ForbidHttpResult).ShouldBeTrue();
    [Fact] void should_report_invalid_request() => _results.Cast<ForbidHttpResult>().All(result => result.Properties!.Items[OpenIddictServerAspNetCoreConstants.Properties.Error] == "invalid_request").ShouldBeTrue();
    [Fact] void should_explain_that_max_age_is_unsupported() => _results.Cast<ForbidHttpResult>().All(result => result.Properties!.Items[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] == "max_age is not supported").ShouldBeTrue();
}

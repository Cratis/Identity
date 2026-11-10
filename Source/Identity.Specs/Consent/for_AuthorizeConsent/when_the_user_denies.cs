// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http.HttpResults;
using OpenIddict.Server.AspNetCore;

namespace Cratis.Identity.ConsentFlow.for_AuthorizeConsent;

public class when_the_user_denies : given.AuthorizeConsentDriver
{
    Task Because() => Submit(Deny);

    [Fact] void should_refuse_with_an_oauth_error() => Result.ShouldBeOfExactType<ForbidHttpResult>();
    [Fact] void should_report_access_denied() => ((ForbidHttpResult)Result).Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.Error].ShouldEqual("access_denied");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http.HttpResults;
using OpenIddict.Server.AspNetCore;

namespace Cratis.Identity.ConsentFlow.for_AuthorizeConsent;

public class when_asking_for_consent_without_interaction : given.AuthorizeConsentDriver
{
    Task Because() => Submit(null, prompt: "none");

    [Fact] void should_refuse() => Result.ShouldBeOfExactType<ForbidHttpResult>();
    [Fact] void should_require_consent() => ((ForbidHttpResult)Result).Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.Error].ShouldEqual("consent_required");
}

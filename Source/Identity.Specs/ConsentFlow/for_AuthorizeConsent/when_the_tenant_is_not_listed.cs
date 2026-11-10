// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http.HttpResults;

namespace Cratis.Identity.ConsentFlow.for_AuthorizeConsent;

public class when_the_tenant_is_not_listed : given.AuthorizeConsentDriver
{
    Task Because() => Submit(Allow, tenant: "other");

    [Fact] void should_render_the_page_again() => Result.ShouldBeOfExactType<ContentHttpResult>();
}

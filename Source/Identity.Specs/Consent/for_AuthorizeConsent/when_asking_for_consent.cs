// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http.HttpResults;

namespace Cratis.Identity.ConsentFlow.for_AuthorizeConsent;

public class when_asking_for_consent : given.AuthorizeConsentDriver
{
    Task Because() => Submit(null);

    [Fact] void should_render_the_consent_page() => ((ContentHttpResult)Result).ContentType.ShouldEqual("text/html; charset=utf-8");
}

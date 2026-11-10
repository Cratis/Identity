// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http.HttpResults;

namespace Cratis.Identity.ConsentFlow.for_AuthorizeConsent;

public class when_the_user_allows : given.AuthorizeConsentDriver
{
    Task Because() => Submit(Allow);

    [Fact] void should_issue_directly_from_the_endpoint() => Result.ShouldBeOfExactType<SignInHttpResult>();
    [Fact] void should_not_depend_on_the_consent_activation_core() => ConsentCoreTypes.In(Endpoint).ShouldBeEmpty();
}

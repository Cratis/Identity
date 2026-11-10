// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http.HttpResults;

namespace Cratis.Identity.ConsentFlow.for_AuthorizeConsent;

public class when_the_form_is_forged : given.AuthorizeConsentDriver
{
    Task Because() => Submit(Allow, validForm: false);

    [Fact] void should_answer_bad_request() => Result.ShouldBeOfExactType<BadRequest<string>>();
}

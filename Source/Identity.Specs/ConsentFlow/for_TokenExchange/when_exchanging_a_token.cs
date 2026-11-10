// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization.TokenExchange;
using Microsoft.AspNetCore.Http.HttpResults;
using OpenIddict.Abstractions;

namespace Cratis.Identity.ConsentFlow.for_TokenExchange;

public class when_exchanging_a_token : for_TokenExchangeGrant.given.a_token_exchange_request
{
    async Task Because() => await Handle();

    [Fact] void should_issue_a_token() => _result.ShouldBeOfExactType<SignInHttpResult>();
    [Fact] void should_create_no_authorization() => Issued.GetAuthorizationId().ShouldBeNull();
    [Fact] void should_have_no_consent_or_authorization_dependency() => Dependencies.Where(IsConsentOrAuthorization).ShouldBeEmpty();

    static IEnumerable<Type> Dependencies => typeof(TokenExchangeGrant).GetConstructors().SelectMany(_ => _.GetParameters()).Select(_ => _.ParameterType);

    static bool IsConsentOrAuthorization(Type type) => type.Namespace == typeof(ConsentActivation).Namespace || type.Name.Contains("Consent", StringComparison.Ordinal) || type.Name.Contains("Authorization", StringComparison.Ordinal);
}

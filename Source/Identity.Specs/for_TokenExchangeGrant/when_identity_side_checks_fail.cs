// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Authorization.TokenExchange;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_TokenExchangeGrant;

[PerFactSpecificationLifecycle]
public class when_identity_side_checks_fail : given.a_token_exchange_request
{
    [Fact]
    async Task should_refuse_a_token_the_validator_rejects_with_the_common_description()
    {
        _validator.Validate(default!, default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(Task.FromResult(SubjectTokenResult.Refused(SubjectTokenRefusal.Invalid)));
        await Handle();
        Error.ShouldEqual(Errors.InvalidGrant);
        Description.ShouldEqual(TokenExchangeGrant.InvalidSubjectDescription);
    }

    [Fact]
    async Task should_refuse_a_subject_that_is_not_linked()
    {
        _keys.Find(default!, default!).ReturnsForAnyArgs(Task.FromResult<UpstreamKeyMatch?>(null));
        await Handle();
        Error.ShouldEqual(Errors.InvalidGrant);
        Description.ShouldEqual(TokenExchangeGrant.InvalidSubjectDescription);
    }

    [Fact]
    async Task should_refuse_an_account_that_is_not_active()
    {
        _accounts.FindActive(default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(Task.FromResult<Account?>(null));
        await Handle();
        Error.ShouldEqual(Errors.InvalidGrant);
        Description.ShouldEqual(TokenExchangeGrant.InvalidSubjectDescription);
    }

    [Fact]
    async Task should_refuse_a_tenant_the_account_cannot_select()
    {
        var result = await Run(_ => _.SetParameter(TokenExchangeConstants.Tenant, "other"));
        ErrorOf(result).ShouldEqual(Errors.InvalidGrant);
        DescriptionOf(result).ShouldEqual(TokenExchangeGrant.InvalidSubjectDescription);
    }
}

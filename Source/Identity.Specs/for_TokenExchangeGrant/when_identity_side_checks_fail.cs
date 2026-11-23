// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Accounts.Lookup;
using Cratis.Identity.Authorization.TokenExchange;
using Cratis.Identity.Contracts.Accounts;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_TokenExchangeGrant;

[PerFactSpecificationLifecycle]
public class when_identity_side_checks_fail : given.a_token_exchange_request
{
    [Fact]
    async Task should_refuse_a_token_the_validator_rejects_with_the_common_description()
    {
        _validator.Validate(default!, default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(Task.FromResult(SubjectTokenResult.Refused(SubjectTokenRefusal.Invalid)));
        await ShouldRefuseBeforeTenantLookup();
    }

    [Fact]
    async Task should_refuse_a_subject_that_is_not_linked()
    {
        _keys.Find(default!, default!).ReturnsForAnyArgs(Task.FromResult<UpstreamKeyMatch?>(null));
        await ShouldRefuseBeforeTenantLookup();
    }

    [Fact]
    async Task should_refuse_an_inactive_link_for_the_exact_verified_subject()
    {
        _keys.Find(_subject.Issuer, _subject.Subject).Returns(Task.FromResult<UpstreamKeyMatch?>(new(_account.Id, default!, false)));
        await ShouldRefuseBeforeTenantLookup();
    }

    [Fact]
    async Task should_not_use_a_match_for_another_issuer_with_the_same_subject()
    {
        _keys.Find(default!, default!).ReturnsForAnyArgs(Task.FromResult<UpstreamKeyMatch?>(null));
        _keys.Find(new UpstreamIssuer("https://other.example"), _subject.Subject).Returns(Task.FromResult<UpstreamKeyMatch?>(new(_account.Id, default!, true)));
        await ShouldRefuseBeforeTenantLookup();
        await _keys.Received(1).Find(_subject.Issuer, _subject.Subject);
    }

    [Fact]
    async Task should_refuse_an_account_that_is_not_active()
    {
        _accounts.FindActive(default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(Task.FromResult<Account?>(null));
        await ShouldRefuseBeforeTenantLookup();
    }

    [Fact]
    async Task should_refuse_a_tenant_the_account_cannot_select()
    {
        var result = await Run(_ => _.SetParameter(TokenExchangeConstants.Tenant, "other"));
        ErrorOf(result).ShouldEqual(Errors.InvalidGrant);
        DescriptionOf(result).ShouldEqual(TokenExchangeGrant.InvalidSubjectDescription);
    }

    async Task ShouldRefuseBeforeTenantLookup()
    {
        await Handle();
        Error.ShouldEqual(Errors.InvalidGrant);
        Description.ShouldEqual(TokenExchangeGrant.InvalidSubjectDescription);
        await _tenants.DidNotReceiveWithAnyArgs().SelectableTenants(default!, default!, default);
    }
}

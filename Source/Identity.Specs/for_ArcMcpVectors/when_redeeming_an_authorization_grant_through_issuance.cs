// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.IdentityModel.JsonWebTokens;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_ArcMcpVectors;

public class when_redeeming_an_authorization_grant_through_issuance : given.an_authorization_code
{
    (string[] AuthorizedAudiences, JsonWebToken? Token, string? Error) _explicit;
    (string[] AuthorizedAudiences, JsonWebToken? Token, string? Error) _inferred;
    (string[] AuthorizedAudiences, JsonWebToken? Token, string? Error) _retargeted;
    (string[] AuthorizedAudiences, JsonWebToken? Token, string? Error) _scopeRejected;

    async Task Because()
    {
        _explicit = await RedeemThroughIssuance(false, Resource);
        _inferred = await RedeemThroughIssuance(true, default);
        _retargeted = await RedeemThroughIssuance(true, "https://studio.example.test/mcp");
        _scopeRejected = await RedeemThroughIssuance(true, default, validateAttackerScope: true);
    }

    [Fact] void should_preserve_the_explicit_authorization_audience_despite_attacker_scopes() => BoundToAuthorization(_explicit);
    [Fact] void should_preserve_the_inferred_authorization_audience_despite_attacker_scopes() => BoundToAuthorization(_inferred);
    [Fact] void should_reject_an_attacker_resource() => _retargeted.Error.ShouldEqual(Errors.InvalidTarget);
    [Fact] void should_not_issue_a_token_for_an_attacker_resource() => _retargeted.Token.ShouldBeNull();
    [Fact] void should_reject_attacker_scopes_at_native_code_redemption_validation() => _scopeRejected.Error.ShouldEqual(Errors.InvalidRequest);
    [Fact] void should_not_issue_a_token_when_native_validation_rejects_scopes() => _scopeRejected.Token.ShouldBeNull();

    void BoundToAuthorization((string[] AuthorizedAudiences, JsonWebToken? Token, string? Error) result)
    {
        result.Error.ShouldBeNull();
        result.AuthorizedAudiences.ShouldEqual([Audience]);
        result.Token.ShouldNotBeNull();
        result.Token.Audiences.ShouldEqual(result.AuthorizedAudiences);
        result.Token.GetClaim(Claims.Scope).Value.ShouldEqual("direct:read");
    }
}

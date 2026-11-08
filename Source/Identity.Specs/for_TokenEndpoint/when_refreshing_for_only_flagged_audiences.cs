// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_TokenEndpoint;

public class when_refreshing_for_only_flagged_audiences : RevocationSpecs.given.a_contended_refresh
{
    ClaimsPrincipal _principal;

    async Task Because() => _principal = ((SignInHttpResult)await ReachTokenEndpoint("1700000000", new Dictionary<string, bool> { ["audience"] = true, ["other"] = true })).Principal;

    [Fact] void should_carry_the_idp_sub() => _principal.FindFirst(IdentityClaims.IdpSub).Value.ShouldEqual("42");
    [Fact] void should_carry_the_github_id() => _principal.FindFirst(IdentityClaims.GitHubId).Value.ShouldEqual("42");
    [Fact] void should_carry_the_preferred_username() => _principal.FindFirst(Claims.PreferredUsername).Value.ShouldEqual("member");
    [Fact] void should_carry_the_display_name_as_name() => _principal.FindFirst(Claims.Name).Value.ShouldEqual("Member");
}

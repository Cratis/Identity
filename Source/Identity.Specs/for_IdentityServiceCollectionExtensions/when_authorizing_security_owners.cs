// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Authorization;
using Cratis.Identity.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.for_IdentityServiceCollectionExtensions;

public class when_authorizing_security_owners : Specification
{
    [Theory]
    [InlineData(true, true, "owner-subject", true)]
    [InlineData(true, true, "other-subject", false)]
    [InlineData(true, true, null, false)]
    [InlineData(true, false, "owner-subject", false)]
    [InlineData(false, true, "owner-subject", false)]
    public async Task should_allow_only_a_configured_authenticated_owner(bool configured, bool authenticated, string? subject, bool allowed)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<IdentityOptions>(options => options.SecurityOwners = configured ? ["owner-subject"] : []);
        services.AddIdentityAuthorization();
        await using var provider = services.BuildServiceProvider();
        var identity = new ClaimsIdentity(subject is null ? [] : [new Claim(Claims.Subject, subject)], authenticated ? IdentitySchemes.Session : null);
        var result = await provider.GetRequiredService<IAuthorizationService>().AuthorizeAsync(new ClaimsPrincipal(identity), null, IdentityPolicies.IssuerSecurityOwner);
        result.Succeeded.ShouldEqual(allowed);
    }
}

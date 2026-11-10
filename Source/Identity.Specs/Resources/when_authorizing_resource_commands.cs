// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Testing.Commands;
using Cratis.Identity.Accounts.given;
using Cratis.Identity.Authorization;
using Cratis.Identity.Configuration;
using Cratis.Identity.Contracts.Resources;
using Cratis.Identity.Hosting;
using Cratis.Identity.Resources.Registering;
using Cratis.Identity.Resources.RegisteringScope;
using Cratis.Identity.Resources.RetiringScope;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Resources;

public class when_authorizing_resource_commands : Specification
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task should_deny_every_command_before_handling(bool anonymous)
    {
        var identity = anonymous ? new ClaimsIdentity() : new ClaimsIdentity([new Claim(Claims.Subject, "other-subject"), new Claim(ClaimTypes.Role, "issuer-security-owner")], IdentitySchemes.Session);
        var principal = new ClaimsPrincipal(identity);
        var id = ResourceId.New();
        await Denied(new RegisterResource("https://api.example/", "api", "api", new("API")), principal);
        await Denied(new RegisterResourceScope(id, "api:read", new("Read")), principal);
        await Denied(new RetireResourceScope(id, "api:read"), principal);
    }

    [Theory]
    [InlineData(typeof(RegisterResource))]
    [InlineData(typeof(RegisterResourceScope))]
    [InlineData(typeof(RetireResourceScope))]
    public async Task should_require_the_configured_owner_subject_not_a_role(Type command)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<IdentityOptions>(options => options.SecurityOwners = ["owner-subject"]);
        services.AddIdentityAuthorization();
        await using var provider = services.BuildServiceProvider();
        var policy = command.GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Policy;
        policy.ShouldEqual(IdentityPolicies.IssuerSecurityOwner);
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var owner = new ClaimsPrincipal(new ClaimsIdentity([new Claim(Claims.Subject, "owner-subject")], IdentitySchemes.Session));
        (await authorization.AuthorizeAsync(owner, null, policy)).Succeeded.ShouldBeTrue();
    }

    static async Task Denied<T>(T command, ClaimsPrincipal principal)
        where T : class
    {
        await using var scenario = new CommandScenario<T>();
        scenario.Services.Configure<IdentityOptions>(options => options.SecurityOwners = ["owner-subject"]);
        scenario.Services.AddIdentityAuthorization();
        scenario.Services.AddSingleton<IAuthorizationAttributeEvaluator, AspNetAuthorizationAttributeEvaluator>();
        scenario.Services.AddSingleton<IAnonymousEvaluator, AspNetAnonymousEvaluator>();
        scenario.Services.AddSingleton<ArcAuthorizationPolicyRuntime>();
        scenario.Services.AddSingleton<IAuthorizationPolicyRuntime, AspNetAuthorizationPolicyRuntime>();
        scenario.Services.AddSingleton<ICurrentPrincipalAccessor>(new FixedAccountPrincipal(principal));
        var result = await scenario.Execute(command, TestContext.Current.CancellationToken);
        result.IsAuthorized.ShouldBeFalse();
        result.ShouldNotHaveExceptions();
    }
}

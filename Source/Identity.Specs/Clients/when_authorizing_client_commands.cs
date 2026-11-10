// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Testing.Commands;
using Cratis.Identity.Accounts.given;
using Cratis.Identity.Authorization;
using Cratis.Identity.Clients.ChangingRedirectUris;
using Cratis.Identity.Clients.Registering;
using Cratis.Identity.Clients.Retiring;
using Cratis.Identity.Clients.RotatingSecret;
using Cratis.Identity.Configuration;
using Cratis.Identity.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Cratis.Identity.Clients;

public class when_authorizing_client_commands : given.client_commands
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task should_deny_every_command_before_handling(bool anonymous)
    {
        var identity = anonymous ? new ClaimsIdentity() : new ClaimsIdentity([new Claim(Claims.Subject, "other-subject"), new Claim(ClaimTypes.Role, "issuer-security-owner")], IdentitySchemes.Session);
        var principal = new ClaimsPrincipal(identity);
        await Denied(new RegisterClient(new("spec-client"), _configuration, Plaintext), principal);
        await Denied(new ChangeRedirectUris(_id, ["https://client.example/callback"]), principal);
        await Denied(new RotateSecret(_id, Plaintext, new(Now, Now)), principal);
        await Denied(new RetireClient(_id), principal);
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

    [Theory]
    [InlineData(typeof(RegisterClient))]
    [InlineData(typeof(ChangeRedirectUris))]
    [InlineData(typeof(RotateSecret))]
    [InlineData(typeof(RetireClient))]
    public async Task should_deny_anonymous_and_non_owner_even_with_an_owner_role(Type command)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<IdentityOptions>(options => options.SecurityOwners = ["owner-subject"]);
        services.AddIdentityAuthorization();
        await using var provider = services.BuildServiceProvider();
        var policy = command.GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Policy;
        policy.ShouldEqual(IdentityPolicies.IssuerSecurityOwner);
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        foreach (var principal in new[]
        {
            new ClaimsPrincipal(new ClaimsIdentity()),
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(Claims.Subject, "other-subject"), new Claim(ClaimTypes.Role, "issuer-security-owner")], IdentitySchemes.Session))
        })
        {
            (await authorization.AuthorizeAsync(principal, null, policy)).Succeeded.ShouldBeFalse();
        }

        var owner = new ClaimsPrincipal(new ClaimsIdentity([new Claim(Claims.Subject, "owner-subject")], IdentitySchemes.Session));
        (await authorization.AuthorizeAsync(owner, null, policy)).Succeeded.ShouldBeTrue();
    }
}

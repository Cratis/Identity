// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Tenants;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;

namespace Cratis.Identity.ConsentFlow.given;

/// <summary>
/// Spec-side seam over the live authorize endpoint (the consent POST), driven through plain calls without a host.
/// </summary>
public class AuthorizeConsentDriver : for_AccessTokenIdentity.given.an_account_and_a_grant
{
    internal const string Allow = ConsentForm.Allow;
    internal const string Deny = ConsentForm.Deny;

    internal IResult Result;
    internal AuthorizationEndpoint Endpoint;

    /// <summary>Submits the authorize request, optionally carrying a consent decision.</summary>
    /// <param name="decision">The submitted consent decision, or null for the initial request.</param>
    /// <param name="tenant">The submitted tenant.</param>
    /// <param name="validForm">Whether the antiforgery check accepts the submission.</param>
    /// <param name="prompt">The OAuth prompt value.</param>
    /// <returns>The completion.</returns>
    internal async Task Submit(string? decision, string tenant = "cratis", bool validForm = true, string? prompt = null)
    {
        var accounts = Substitute.For<IAccountReader>();
        accounts.FindActive(Arg.Any<AccountSubject>(), Arg.Any<CancellationToken>()).Returns(_account);
        var authentication = Substitute.For<IAuthenticationService>();
        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), Arg.Any<string>())
            .Returns(AuthenticateResult.Success(new(SessionPrincipal.For(_account, "github", AuthTime), "test")));
        var policy = Substitute.For<IGrantPolicy>();
        policy.Evaluate(Arg.Any<GrantRequest>()).Returns(new Grantable(_grant));
        var tenants = Substitute.For<ITenantDirectory>();
        tenants.SelectableTenants(Arg.Any<Account>(), Arg.Any<Resources.ResourceServer>(), Arg.Any<CancellationToken>()).Returns(new TenantsListed([new("cratis", "Cratis")]));
        var antiforgery = Substitute.For<IAntiforgery>();
        antiforgery.GetAndStoreTokens(Arg.Any<HttpContext>()).Returns(new AntiforgeryTokenSet("request-token", "cookie-token", "form", "header"));
        antiforgery.IsRequestValidAsync(Arg.Any<HttpContext>()).Returns(validForm);
        var applications = Substitute.For<IOpenIddictApplicationManager>();
        var client = new object();
        applications.FindByClientIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<object?>(client));
        applications.GetDisplayNameAsync(client, Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<string?>("Client"));

        Endpoint = new(new SignedInAccounts(accounts, Options.Create(new UpstreamLoginOptions())), policy, tenants, new ConsentPages(antiforgery), applications, NullLogger<AuthorizationEndpoint>.Instance);

        var request = new OpenIddictRequest { ClientId = ClientId, RedirectUri = "https://client.example/callback", Scope = "direct:read", Prompt = prompt };
        await using var services = new ServiceCollection().AddSingleton(authentication).BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services };
        http.Features.Set(new OpenIddictServerAspNetCoreFeature { Transaction = new OpenIddictServerTransaction { Request = request, Logger = NullLogger.Instance, Options = new OpenIddictServerOptions() } });
        if (decision is not null)
        {
            http.Request.Method = HttpMethods.Post;
            http.Request.ContentType = "application/x-www-form-urlencoded";
            http.Request.Form = new FormCollection(new()
            {
                [ConsentForm.Decision] = decision,
                [ConsentForm.Tenant] = tenant
            });
        }

        Result = await Endpoint.Handle(http);
    }
}

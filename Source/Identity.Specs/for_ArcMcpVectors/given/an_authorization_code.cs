// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Resources;
using Cratis.Identity.Revocation;
using Cratis.Identity.Tenants;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Cratis.Identity.for_ArcMcpVectors.given;

public class an_authorization_code : an_authorization_server
{
    readonly IAuthenticationService _authentication = Substitute.For<IAuthenticationService>();

    protected override void ConfigureServices()
    {
        var services = Builder.Services;
        var account = Identity.given.GitHubAccounts.Create(new("7d9d5c9e-0000-4000-8000-000000000001"), "Synthetic Member", null, "42", "synthetic-member");
        var accounts = Substitute.For<IAccountReader>();
        accounts.FindActive(Arg.Any<AccountSubject>(), Arg.Any<CancellationToken>()).Returns(account);
        var tenants = Substitute.For<ITenantDirectory>();
        tenants.SelectableTenants(account, Arg.Any<ResourceServer>(), Arg.Any<CancellationToken>())
            .Returns(new TenantsListed([new("synthetic-tenant", "Synthetic Tenant")]));
        var session = SessionPrincipal.For(account, "synthetic", DateTimeOffset.UtcNow);
        _authentication.AuthenticateAsync(Arg.Any<HttpContext>(), IdentitySchemes.Session)
            .Returns(AuthenticateResult.Success(new(session, IdentitySchemes.Session)));
        var antiforgery = Substitute.For<IAntiforgery>();
        antiforgery.IsRequestValidAsync(Arg.Any<HttpContext>()).Returns(true);
        var authorizations = Substitute.For<IOpenIddictAuthorizationManager>();
        var authorization = new object();
        authorizations.FindByIdAsync("synthetic-authorization", Arg.Any<CancellationToken>()).Returns(authorization);
        authorizations.GetPropertiesAsync(authorization, Arg.Any<CancellationToken>()).Returns([]);
        var fences = Substitute.For<IFenceStore>();
        services.AddSingleton(accounts).AddSingleton(tenants).AddSingleton(_authentication)
            .AddSingleton(antiforgery).AddSingleton(fences).AddSingleton(authorizations);

        // Native validation admits only authorization_code here; device/exchange branches are not used.
        services.AddTransient(provider => new TokenEndpoint(accounts, provider.GetRequiredService<IGrantPolicy>(), provider.GetRequiredService<IResourceCatalog>(), tenants, NullLogger<TokenEndpoint>.Instance, fences, null!, null!));
        services.AddOpenIddict().AddServer(server => server
            .AddEventHandler<ApplyAuthorizationResponseContext>(handler => handler.SetOrder(int.MaxValue).UseInlineHandler(context =>
            {
                context.HandleRequest();
                return default;
            })));
    }

    protected async Task<(string[] AuthorizedAudiences, JsonWebToken? Token, string? Error)> RedeemThroughIssuance(bool inferResource, OpenIddictParameter resource, bool validateAttackerScope = false)
    {
        var authorization = await AuthorizeCode(inferResource);
        var audiences = authorization.Principal.GetResources().ToArray();
        var validation = await ValidateRedemption(authorization.AuthorizationCode, resource, validateAttackerScope);
        if (validation.IsRejected) return (audiences, null, validation.Error);

        // OpenIddict rejects scope on code redemption. Exercise endpoint defense in depth only after
        // real encrypted-code/PKCE validation; never replace the principal recovered from that code.
        validation.Request.Scope = "studio:read";
        var granted = validation.Transaction.GetProperty<ProcessAuthenticationContext>(typeof(ProcessAuthenticationContext).FullName).AuthorizationCodePrincipal;
        _authentication.AuthenticateAsync(Arg.Any<HttpContext>(), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)
            .Returns(AuthenticateResult.Success(new(granted, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)));
        var result = await Provider.GetRequiredService<TokenEndpoint>().Handle(Http(validation.Transaction));
        if (result is ForbidHttpResult forbidden)
        {
            return (audiences, null, forbidden.Properties.Items[OpenIddictServerAspNetCoreConstants.Properties.Error]);
        }

        result.ShouldBeOfExactType<SignInHttpResult>();
        var issuance = new ProcessSignInContext(validation.Transaction)
        {
            Principal = ((SignInHttpResult)result).Principal,
            GenerateAccessToken = true
        };
        await Provider.GetRequiredService<IOpenIddictServerDispatcher>().DispatchAsync(issuance);
        issuance.IsRejected.ShouldBeFalse();
        return (audiences, new JsonWebToken(issuance.AccessToken), null);
    }

    async Task<ProcessSignInContext> AuthorizeCode(bool inferResource)
    {
        var request = new OpenIddictRequest
        {
            ClientId = "cratis-cli",
            RedirectUri = "http://127.0.0.1/callback",
            ResponseType = ResponseTypes.Code,
            Scope = "direct:read",
            CodeChallenge = Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(Verifier))),
            CodeChallengeMethod = CodeChallengeMethods.Sha256
        };
        if (!inferResource) request.AddParameter(Parameters.Resource, Resource);
        var transaction = Transaction(request, OpenIddictServerEndpointType.Authorization);
        var validation = new ValidateAuthorizationRequestContext(transaction);
        await Provider.GetRequiredService<IOpenIddictServerDispatcher>().DispatchAsync(validation);
        validation.IsRejected.ShouldBeFalse();
        var http = Http(transaction);
        http.Request.Method = HttpMethods.Post;
        http.Request.ContentType = "application/x-www-form-urlencoded";
        http.Request.Form = new FormCollection(new Dictionary<string, StringValues>
        {
            [ConsentForm.Decision] = ConsentForm.Allow,
            [ConsentForm.Tenant] = "synthetic-tenant"
        });
        var result = await Provider.GetRequiredService<AuthorizationEndpoint>().Handle(http);
        result.ShouldBeOfExactType<SignInHttpResult>();
        var principal = ((SignInHttpResult)result).Principal;

        // Authorization storage is disabled in this fixture; retain its identity at that persistence boundary.
        principal.SetAuthorizationId("synthetic-authorization");
        var issuance = new ProcessSignInContext(transaction) { Principal = principal, GenerateAuthorizationCode = true };
        await Provider.GetRequiredService<IOpenIddictServerDispatcher>().DispatchAsync(issuance);
        issuance.ErrorDescription.ShouldBeNull();
        issuance.Error.ShouldBeNull();
        issuance.AuthorizationCode.ShouldNotBeNull();
        return issuance;
    }

    async Task<ValidateTokenRequestContext> ValidateRedemption(string code, OpenIddictParameter resource, bool attackerScope)
    {
        var request = new OpenIddictRequest
        {
            GrantType = GrantTypes.AuthorizationCode,
            ClientId = "cratis-cli",
            Code = code,
            CodeVerifier = Verifier,
            RedirectUri = "http://127.0.0.1/callback",
            Scope = attackerScope ? "studio:read" : null
        };
        if (resource != default) request.AddParameter(Parameters.Resource, resource);
        var validation = new ValidateTokenRequestContext(Transaction(request, OpenIddictServerEndpointType.Token));
        await Provider.GetRequiredService<IOpenIddictServerDispatcher>().DispatchAsync(validation);
        return validation;
    }

    DefaultHttpContext Http(OpenIddictServerTransaction transaction)
    {
        var http = new DefaultHttpContext { RequestServices = Provider };
        http.Features.Set(new OpenIddictServerAspNetCoreFeature { Transaction = transaction });
        return http;
    }
}

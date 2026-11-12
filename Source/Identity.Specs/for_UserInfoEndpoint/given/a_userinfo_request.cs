// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization;
using Cratis.Identity.Configuration;
using Cratis.Identity.Contracts.Accounts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Cratis.Identity.for_UserInfoEndpoint.given;

public class a_userinfo_request : for_OidcIdentity.given.an_identity_grant
{
    ServiceProvider _provider;
    UserInfoEndpoint _endpoint;
    private protected IAccountReader _reader;
    protected HttpContext _context;
    protected IResult _result;
    protected virtual bool ActiveAccount => true;
    protected virtual bool AuthenticationSucceeds => true;
    protected virtual string Audience => OidcIdentity.UserInfoAudience;
    protected virtual string TokenType => OpenIddictConstants.TokenTypeIdentifiers.AccessToken;

    void Establish()
    {
        var options = new IdentityOptions();
        options.Oidc.IssuanceEnabled = true;
        var identity = OidcIdentity.Create(new(_account, AuthTime, SessionId), ClientId, new IdentityOnlyGrant(["openid", "profile"]), "example-nonce");
        identity.SetAudiences(Audience);
        identity.SetTokenType(TokenType);
        var authentication = Substitute.For<IAuthenticationService>();
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)
            .Returns(AuthenticationSucceeds ? AuthenticateResult.Success(ticket) : AuthenticateResult.Fail("invalid_token"));
        _provider = new ServiceCollection().AddSingleton(authentication).BuildServiceProvider();
        _context = new DefaultHttpContext { RequestServices = _provider };
        _context.Request.Headers.Authorization = "Bearer example-generated-access-token";
        _reader = Substitute.For<IAccountReader>();
        _reader.FindActive(Arg.Any<AccountSubject>(), Arg.Any<CancellationToken>()).Returns(ActiveAccount ? _account : null);
        _endpoint = new(_reader, Options.Create(options));
    }

    protected async Task ReadUserInfo() => _result = await _endpoint.Handle(_context);
    void Destroy() => _provider?.Dispose();
}

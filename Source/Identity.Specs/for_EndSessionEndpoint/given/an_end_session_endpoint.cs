// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Claims;
using Cratis.Identity.Authorization;
using Cratis.Identity.Sessions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.for_EndSessionEndpoint.given;

public class an_end_session_endpoint : for_EndSessionHintValidator.given.a_hint_validator
{
    protected const string RedirectUri = "https://relying-party.example/signed-out?existing=value";
    protected IAuthenticationService authentication;
    protected IAntiforgery antiforgery;
    private protected EndSessionEndpoint endpoint;
    ServiceProvider _services;

    void Establish()
    {
        authentication = Substitute.For<IAuthenticationService>();
        SetSession(Subject, SessionId);
        antiforgery = Substitute.For<IAntiforgery>();
        antiforgery.IsRequestValidAsync(Arg.Any<HttpContext>()).Returns(true);
        antiforgery.GetAndStoreTokens(Arg.Any<HttpContext>()).Returns(new AntiforgeryTokenSet("form-token", "cookie-token", "__logout_antiforgery", "X-CSRF"));
        applications.GetPostLogoutRedirectUrisAsync(application, Arg.Any<CancellationToken>()).Returns(ImmutableArray.Create(RedirectUri));
        _services = new ServiceCollection().AddSingleton(authentication).BuildServiceProvider();
        endpoint = new(validator, applications, antiforgery);
    }

    protected void SetSession(string subject, string sessionId)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", subject), new Claim("sid", sessionId)], "test"));
        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), IdentitySchemes.Session).Returns(AuthenticateResult.Success(new(principal, IdentitySchemes.Session)));
    }

    protected HttpContext Context(string hint = null, string redirect = null, string state = null, string clientId = null, bool post = false)
    {
        var context = new DefaultHttpContext { RequestServices = _services };
        var values = new Dictionary<string, string> { ["id_token_hint"] = hint, ["post_logout_redirect_uri"] = redirect, ["state"] = state, ["client_id"] = clientId };
        context.Request.Path = "/connect/endsession";
        context.Request.Method = post ? HttpMethods.Post : HttpMethods.Get;
        if (post)
        {
            context.Request.ContentType = "application/x-www-form-urlencoded";
            context.Request.Form = new FormCollection(values.Where(_ => _.Value is not null).ToDictionary(_ => _.Key, _ => new Microsoft.Extensions.Primitives.StringValues(_.Value), StringComparer.Ordinal));
        }
        else
        {
            context.Request.QueryString = new(new Uri("https://identity.example" + QueryHelpers.AddQueryString("/connect/endsession", values)).Query);
        }

        return context;
    }

    protected int SignOutCount() => authentication.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(IAuthenticationService.SignOutAsync));

    protected static string Content(IResult result) => ((Microsoft.AspNetCore.Http.HttpResults.ContentHttpResult)result).ResponseContent;

    protected static int? Status(IResult result) => ((IStatusCodeHttpResult)result).StatusCode;

    void Destroy() => _services.Dispose();
}

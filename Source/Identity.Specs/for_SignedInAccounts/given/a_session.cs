// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.given;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace Cratis.Identity.for_SignedInAccounts.given;

public class a_session : Specification
{
    internal IAccountReader _accounts;
    internal ClaimsIdentity _identity;
    internal HttpContext _http;
    internal OpenIddictRequest _request;
    internal SignedInAccounts _finder;
    internal SignedInAccount _result;
    ServiceProvider _services;

    void Establish()
    {
        var time = new FixedTimeProvider(DateTimeOffset.FromUnixTimeSeconds(1_700_000_000));
        var account = new Account(new("7d9d5c9e-0000-4000-8000-000000000001"), "Member", null, AccountStatus.Active, []);
        _accounts = Substitute.For<IAccountReader>();
        _accounts.FindActive(Arg.Any<AccountSubject>(), Arg.Any<CancellationToken>()).Returns(account);
        var principal = SessionPrincipal.For(account, "test", time.Now);
        _identity = (ClaimsIdentity)principal.Identity;
        var authentication = Substitute.For<IAuthenticationService>();
        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), Arg.Any<string>()).Returns(AuthenticateResult.Success(new(principal, "test")));
        _services = new ServiceCollection().AddSingleton(authentication).BuildServiceProvider();
        _http = new DefaultHttpContext { RequestServices = _services };
        _request = new();
        _finder = new(_accounts, Options.Create(new UpstreamLoginOptions()), time);
    }

    void Destroy() => _services.Dispose();

    internal void SetAuthenticationTime(string value)
    {
        _identity.RemoveClaim(_identity.FindFirst(OpenIddictConstants.Claims.AuthenticationTime));
        _identity.AddClaim(new(OpenIddictConstants.Claims.AuthenticationTime, value));
    }
}

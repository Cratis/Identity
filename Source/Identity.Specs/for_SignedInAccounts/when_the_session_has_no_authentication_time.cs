// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Identity.Accounts;
using Cratis.Identity.Contracts.Accounts;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace Cratis.Identity.for_SignedInAccounts;

public class when_the_session_has_no_authentication_time : Specification
{
    const string AccountId = "7d9d5c9e-0000-4000-8000-000000000001";
    SignedInAccount _result;

    async Task Because()
    {
        var account = new Account(new(AccountId), "Member", null, "42", "member", AccountStatus.Active, []);
        var accounts = Substitute.For<IAccountReader>();
        accounts.FindActive(Arg.Any<AccountSubject>(), Arg.Any<CancellationToken>()).Returns(account);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(OpenIddictConstants.Claims.Subject, AccountId)], "test"));
        var authentication = Substitute.For<IAuthenticationService>();
        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), Arg.Any<string>()).Returns(AuthenticateResult.Success(new(principal, "test")));
        await using var services = new ServiceCollection().AddSingleton(authentication).BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services };
        _result = await new SignedInAccounts(accounts, Options.Create(new UpstreamLoginOptions()), TimeProvider.System).Find(http, new OpenIddictRequest());
    }

    [Fact] void should_require_a_new_sign_in() => _result.ShouldBeNull();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using System.Security.Claims;
using Cratis.Chronicle;
using Cratis.Identity.Authorization;
using Cratis.Identity.Contracts.Accounts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.Integration.for_NativeAccounts;

[Collection(IdentityCollection.Name)]
public class when_posting_the_protocol_command_over_http(IdentityHost host)
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task should_refuse_without_appending_events(bool authenticated, bool validate)
    {
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if (authenticated)
        {
            var options = host.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(IdentitySchemes.Session);
            var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", AccountSubject.New().Value)], IdentitySchemes.Session));
            var ticket = new AuthenticationTicket(principal, IdentitySchemes.Session);
            client.DefaultRequestHeaders.Add("Cookie", $"{options.Cookie.Name}={options.TicketDataFormat.Protect(ticket)}");
        }

        await using var scope = host.Services.CreateAsyncScope();
        var log = scope.ServiceProvider.GetRequiredService<IEventStore>().EventLog;
        var before = await log.GetTailSequenceNumber();
        var route = "/api/accounts/signing-in/sign-in-with-upstream-key" + (validate ? "/validate" : string.Empty);
        using var response = await client.PostAsJsonAsync(route, new { OperationId = FactId.New().Value }, TestContext.Current.CancellationToken);
        (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden).ShouldBeTrue();
        (await log.GetTailSequenceNumber()).ShouldEqual(before);
        response.Headers.Contains("Set-Cookie").ShouldBeFalse();
    }
}

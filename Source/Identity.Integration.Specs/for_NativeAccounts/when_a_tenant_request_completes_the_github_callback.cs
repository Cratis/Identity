// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Registrations;
using Cratis.Identity.Accounts;
using Cratis.Identity.Authorization;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Integration.for_NativeAccounts;

[Collection(IdentityCollection.Name)]
public class when_a_tenant_request_completes_the_github_callback(IdentityHost host)
{
    [Theory]
    [InlineData("tenant-alpha")]
    [InlineData("tenant-beta")]
    public async Task should_read_the_realm_account_during_the_real_oauth_callback(string tenant)
    {
        using var upstream = new HttpClient(new GitHubBackchannel());
        await using var oauthHost = host.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<UpstreamLoginOptions>(options => options.Scheme = IdentitySchemes.GitHub);
            services.PostConfigure<OAuthOptions>(IdentitySchemes.GitHub, options => options.Backchannel = upstream);
        }));
        await IdentityHost.WaitForRecovery(oauthHost.Services);
        using var client = oauthHost.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri(IdentityHost.Issuer)
        });
        client.DefaultRequestHeaders.Add("x-cratis-tenant-id", tenant);
        var browser = new Browser(client);
        using var challenge = await browser.Authorize(Browser.CliAuthorization(Browser.Pkce().Challenge));
        challenge.StatusCode.ShouldEqual(HttpStatusCode.Redirect);
        var redirect = challenge.Headers.Location ?? throw new InvalidOperationException("Expected the GitHub challenge redirect.");
        redirect.Host.ShouldEqual("github.com");
        var state = QueryHelpers.ParseQuery(redirect.Query)["state"].ToString();
        state.ShouldNotBeEmpty();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/signin-github?code=spec-code&state={Uri.EscapeDataString(state)}");
        using var callback = await browser.Send(request);
        callback.StatusCode.ShouldEqual(HttpStatusCode.OK);
        (await callback.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain("Allow");
        using var mine = await client.GetAsync("/api/accounts/mine", TestContext.Current.CancellationToken);
        mine.StatusCode.ShouldEqual(HttpStatusCode.OK);
        var result = await mine.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var subject = result.GetProperty("data").GetProperty("id").GetString();
        Guid.TryParse(subject, out _).ShouldBeTrue();

        await using var scope = oauthHost.Services.CreateAsyncScope();
        var chronicle = scope.ServiceProvider.GetRequiredService<IChronicleClient>();
        var realm = await chronicle.GetEventStore("Identity", EventStoreNamespaceName.Default);
        var tenantStore = await chronicle.GetEventStore("Identity", tenant);
        (await tenantStore.WaitForRegistration(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();
        var stored = await realm.ReadModels.GetInstanceById<Account>(subject);
        stored.ShouldNotBeNull();
        stored.Id.Value.ShouldEqual(subject);
        (await realm.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, subject)).ShouldNotBeEmpty();
        (await tenantStore.ReadModels.GetInstanceById<Account>(subject)).ShouldBeNull();
        (await tenantStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, subject)).ShouldBeEmpty();
    }

    sealed class GitHubBackchannel : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = request.Method == HttpMethod.Post
                ? JsonContent.Create(new { access_token = "spec-oauth-token", token_type = "bearer" })
                : JsonContent.Create(new { id = FakeUpstream.DefaultUserId, login = "einari", name = "einari" });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }
    }
}

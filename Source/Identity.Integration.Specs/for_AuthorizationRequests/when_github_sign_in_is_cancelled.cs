// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Authorization;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Identity.Integration.for_AuthorizationRequests;

[Collection(IdentityCollection.Name)]
public class when_github_sign_in_is_cancelled(IdentityHost host) : IAsyncLifetime
{
    Uri[] _redirects = [];
    HttpStatusCode _missingStateStatus;
    string _missingStateBody = string.Empty;

    public async ValueTask InitializeAsync()
    {
        await using var factory = host.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.PostConfigure<UpstreamLoginOptions>(options => options.Scheme = IdentitySchemes.GitHub)));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri(IdentityHost.Issuer)
        });
        var browser = new Browser(client);
        var redirects = new List<Uri>();
        foreach (var error in new[] { "access_denied", "server_error" })
        {
            var (_, challenge) = Browser.Pkce();
            using var github = await browser.Authorize(Browser.CliAuthorization(challenge));
            var state = QueryHelpers.ParseQuery(github.Headers.Location!.Query)["state"].ToString();
            using var result = await browser.Send(new HttpRequestMessage(HttpMethod.Get, $"/signin-github?error={error}&state={Uri.EscapeDataString(state)}"));
            redirects.Add(result.Headers.Location!);
        }

        _redirects = redirects.ToArray();
        using var missingState = await client.GetAsync("/signin-github?error=provider-secret-error");
        _missingStateStatus = missingState.StatusCode;
        _missingStateBody = await missingState.Content.ReadAsStringAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact] void should_return_access_denied_to_the_validated_client() => _redirects.All(_ => QueryHelpers.ParseQuery(_.Query)["error"].ToString() == "access_denied").ShouldBeTrue();
    [Fact] void should_not_issue_a_code() => _redirects.All(_ => !QueryHelpers.ParseQuery(_.Query).ContainsKey("code")).ShouldBeTrue();
    [Fact] void should_return_a_generic_error_without_valid_challenge_state() => (_missingStateStatus == HttpStatusCode.BadRequest && !_missingStateBody.Contains("provider-secret-error", StringComparison.Ordinal)).ShouldBeTrue();
}

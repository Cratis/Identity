// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Identity.Hosting;
using Cratis.Identity.Upstream;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Identity.for_GitHubSignInEvents;

public class when_returning_to_a_prefixed_authorization_request : Specification
{
    [Theory]
    [InlineData("", "")]
    [InlineData("", "/id")]
    [InlineData("/host", "")]
    [InlineData("/host", "/id")]
    async Task should_return_only_to_the_registered_authorization_path(string pathBase, string prefix)
    {
        var returnUrl = $"{pathBase}{prefix}/connect/authorize?client_id=cratis-cli";
        var context = CreateContext(pathBase, returnUrl);
        await Events(prefix).RemoteFailure(context);
        context.Response.StatusCode.ShouldEqual(StatusCodes.Status302Found);
        context.Response.Headers.Location.ToString().ShouldEqual($"{returnUrl}&upstream_error=access_denied");
        context.Response.Headers.ContainsKey("Set-Cookie").ShouldBeFalse();
    }

    [Theory]
    [InlineData("", "/id", "/connect/authorize?client_id=cratis-cli")]
    [InlineData("", "", "/id/connect/authorize?client_id=cratis-cli")]
    [InlineData("/host", "/id", "/id/connect/authorize?client_id=cratis-cli")]
    [InlineData("", "/id", "https://evil.example/id/connect/authorize?client_id=cratis-cli")]
    [InlineData("", "/id", "//evil.example/id/connect/authorize?client_id=cratis-cli")]
    [InlineData("", "/id", "/id/connect/authorizeevil?client_id=cratis-cli")]
    [InlineData("", "/id", "/id/connect/authorize/../host-work?client_id=cratis-cli")]
    [InlineData("", "/id", "/id/connect/%2e%2e/authorize?client_id=cratis-cli")]
    [InlineData("", "/id", "/id/connect/authorize#client_id=cratis-cli")]
    async Task should_reject_hostile_or_mismatched_return_urls(string pathBase, string prefix, string returnUrl)
    {
        var context = CreateContext(pathBase, returnUrl);
        await Events(prefix).RemoteFailure(context);
        context.Response.StatusCode.ShouldEqual(StatusCodes.Status400BadRequest);
        context.Response.Headers.ContainsKey("Location").ShouldBeFalse();
        context.Response.Headers.ContainsKey("Set-Cookie").ShouldBeFalse();
    }

    static GitHubSignInEvents Events(string prefix) => new(
        Substitute.For<IServiceProvider>(),
        NullLogger<GitHubSignInEvents>.Instance,
        TimeProvider.System,
        Options.Create(new IdentityEndpointOptions { RoutePrefix = prefix }));

    static RemoteFailureContext CreateContext(string pathBase, string returnUrl)
    {
        var http = new DefaultHttpContext();
        http.Request.PathBase = pathBase;
        http.Response.Body = new MemoryStream();
        return new RemoteFailureContext(
            http,
            new AuthenticationScheme("GitHub", "GitHub", typeof(OAuthHandler<OAuthOptions>)),
            new OAuthOptions(),
            new AuthenticationFailureException("Upstream sign-in failed."))
        {
            Properties = new AuthenticationProperties { RedirectUri = returnUrl }
        };
    }
}

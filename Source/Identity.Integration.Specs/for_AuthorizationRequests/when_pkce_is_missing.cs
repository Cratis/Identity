// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Identity.Integration.for_AuthorizationRequests;

/// <summary>
/// OpenIddict rejects this before redirecting, so the error is shown by Identity rather than sent to the client.
/// </summary>
/// <param name="host">The <see cref="IdentityHost"/>.</param>
[Collection(IdentityCollection.Name)]
public class when_pkce_is_missing(IdentityHost host) : IAsyncLifetime
{
    HttpResponseMessage _response;
    string _body;

    public async ValueTask InitializeAsync()
    {
        var browser = host.CreateBrowser();
        var (_, challenge) = Browser.Pkce();
        var parameters = Browser.CliAuthorization(challenge);
        parameters.RemoveAll(_ => _.Key.StartsWith("code_challenge", StringComparison.Ordinal));
        _response = await browser.Authorize(parameters);
        _body = await _response.Content.ReadAsStringAsync();
    }

    public ValueTask DisposeAsync()
    {
        _response.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact] void should_refuse_the_request() => _response.StatusCode.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_name_the_error() => _body.Contains("error:invalid_request", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_redirect_to_the_client() => _response.Headers.Location.ShouldBeNull();
}
